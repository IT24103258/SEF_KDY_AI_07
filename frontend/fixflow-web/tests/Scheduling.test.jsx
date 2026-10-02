import React from 'react';
import { render, screen, waitFor, within, fireEvent } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import fs from 'node:fs';
import path from 'node:path';
import { CalendarPage } from '../src/pages/CalendarPage';
import { WorkOrderDetailPage } from '../src/pages/WorkOrderDetailPage';
import { workOrderApi } from '../src/services/workOrderApi';

vi.mock('../src/services/workOrderApi', () => ({
  workOrderApi: {
    getCalendarEvents: vi.fn(),
    getUsers: vi.fn(),
    getWorkOrderById: vi.fn(),
    updateStatus: vi.fn(),
    approveWorkOrder: vi.fn(),
    rejectWorkOrder: vi.fn(),
    requestRevision: vi.fn(),
    completeWorkOrder: vi.fn(),
    addNote: vi.fn()
  }
}));

// Event anchored to "today" so it always falls inside the rendered week view.
const eventStart = new Date(new Date().setHours(10, 0, 0, 0));
const eventEnd = new Date(eventStart.getTime() + 2 * 60 * 60 * 1000);

const calendarEvent = {
  id: 'wo-1',
  workOrderNumber: 'WO-202609-0001',
  title: 'Ceiling Leak Inspection',
  start: eventStart.toISOString(),
  end: eventEnd.toISOString(),
  priority: 'Critical',
  status: 'Scheduled',
  technicianName: 'Senior Tech',
  conflictDetected: false
};

const baseWorkOrder = (overrides = {}) => ({
  id: 'wo-1',
  workOrderNumber: 'WO-202609-0001',
  title: 'Ceiling Leak Inspection',
  description: 'Inspect the leak in unit 305.',
  status: 'Scheduled',
  priority: 'High',
  conflictDetected: false,
  requestNumber: 'REQ-2026-0001',
  requestTitle: 'Ceiling Leak',
  locationName: 'Tower A',
  building: 'Tower A',
  room: '305',
  technicianName: 'Senior Tech',
  scheduledStartTime: eventStart.toISOString(),
  scheduledEndTime: eventEnd.toISOString(),
  estimatedDurationMinutes: 120,
  aiDecisionSummary: 'Deterministic proposal within business hours.',
  createdAt: eventStart.toISOString(),
  notes: [],
  evidence: [],
  ...overrides
});

const conflictFreeChecklist = {
  scheduleConflictNone: true,
  slaRequirementPassed: true,
  technicianAvailable: true,
  businessHoursValid: true,
  requiredSkillValid: true,
  schemaValid: true
};

const conflictingChecklist = {
  scheduleConflictNone: false,
  slaRequirementPassed: true,
  technicianAvailable: true,
  businessHoursValid: true,
  requiredSkillValid: true,
  schemaValid: true
};

const renderPage = (element) => render(<BrowserRouter>{element}</BrowserRouter>);

beforeEach(() => {
  vi.clearAllMocks();
});

describe('Schedule Board filter layout (CalendarPage)', () => {
  it('renders navigation, search and all three filter selects in one wrapping flex bar', async () => {
    workOrderApi.getCalendarEvents.mockResolvedValue({ success: true, data: [] });
    workOrderApi.getUsers.mockResolvedValue({ success: true, data: [{ id: 'tech-1', name: 'Kamal Perera' }] });

    renderPage(<CalendarPage />);

    const search = screen.getByPlaceholderText('Search...');
    const searchWrap = search.closest('.ff-input-wrap');
    const bar = searchWrap.parentElement;

    expect(bar.style.display).toBe('flex');
    expect(bar.style.flexWrap).toBe('wrap');

    expect(within(bar).getByText('Today')).toBeInTheDocument();
    expect(within(bar).getByText('Previous Week')).toBeInTheDocument();
    expect(within(bar).getByText('Next Week')).toBeInTheDocument();

    const selects = within(bar).getAllByRole('combobox');
    expect(selects).toHaveLength(3);
    selects.forEach((select) => expect(select).toHaveClass('ff-input'));
    expect(within(selects[0]).getByText('All Technicians')).toBeInTheDocument();
    expect(within(selects[1]).getByText('All Priorities')).toBeInTheDocument();
    expect(within(selects[2]).getByText('All Statuses')).toBeInTheDocument();

    // The three filters share one dedicated horizontal container so they cannot
    // split across lines; fixed compact widths stop them stretching to 100%.
    const filterGroup = selects[0].closest('[aria-label="Schedule filters"]');
    expect(filterGroup).not.toBeNull();
    expect(selects[1].closest('[aria-label="Schedule filters"]')).toBe(filterGroup);
    expect(selects[2].closest('[aria-label="Schedule filters"]')).toBe(filterGroup);
    expect(filterGroup.style.display).toBe('flex');
    expect(filterGroup.style.flexWrap).toBe('wrap');
    expect(filterGroup.style.gap).toBe('14px');
    const groupWidths = selects.map((select) => select.closest('.ff-input-wrap').style.flex);
    expect(groupWidths).toEqual(['0 0 240px', '0 0 165px', '0 0 185px']);
  });

  it('filters calendar events by the search text and restores them when cleared', async () => {
    workOrderApi.getCalendarEvents.mockResolvedValue({ success: true, data: [calendarEvent] });
    workOrderApi.getUsers.mockResolvedValue({ success: true, data: [] });

    renderPage(<CalendarPage />);

    await waitFor(() => expect(screen.getByText('Ceiling Leak Inspection')).toBeInTheDocument());

    const search = screen.getByPlaceholderText('Search...');
    fireEvent.change(search, { target: { value: 'plumbing' } });
    await waitFor(() => expect(screen.queryByText('Ceiling Leak Inspection')).not.toBeInTheDocument());

    fireEvent.change(search, { target: { value: 'Ceiling' } });
    await waitFor(() => expect(screen.getByText('Ceiling Leak Inspection')).toBeInTheDocument());
  });
});

describe('Dark mode dropdown theming (theme.css)', () => {
  const themeCss = fs.readFileSync(path.resolve(process.cwd(), 'src/theme.css'), 'utf8');

  it('declares color-scheme for both themes so native popups render themed', () => {
    expect(themeCss).toMatch(/:root\[data-theme='dark'\]\s*\{\s*color-scheme:\s*dark;/);
    expect(themeCss).toMatch(/:root\[data-theme='light'\]\s*\{\s*color-scheme:\s*light;/);
  });

  it('styles select options with theme variables so dark-mode text stays readable', () => {
    expect(themeCss).toMatch(/select\.ff-input option,\s*\n\s*select\.ff-input optgroup\s*\{/);
    const rule = themeCss.match(/select\.ff-input option,\s*\n\s*select\.ff-input optgroup\s*\{([^}]*)\}/);
    expect(rule[1]).toContain('background-color: var(--bg-secondary)');
    expect(rule[1]).toContain('color: var(--text-primary)');
  });
});

describe('Audit tab schedule-conflict checklist (WorkOrderDetailPage)', () => {
  async function openAuditTab(workOrder) {
    workOrderApi.getWorkOrderById.mockResolvedValue({ success: true, data: workOrder });
    renderPage(<WorkOrderDetailPage />);
    await waitFor(() => expect(screen.getByText(/WO-202609-0001/)).toBeInTheDocument());
    fireEvent.click(screen.getByRole('button', { name: 'Audit' }));
  }

  it('shows Schedule Conflicts (not the success label) when the audit checklist records a conflict', async () => {
    await openAuditTab(baseWorkOrder({ conflictDetected: true, validationChecklist: conflictingChecklist }));

    expect(screen.getByText('Schedule Conflicts')).toBeInTheDocument();
    expect(screen.queryByText('No Schedule Conflicts')).not.toBeInTheDocument();
  });

  it('shows No Schedule Conflicts when the audit checklist is conflict-free', async () => {
    await openAuditTab(baseWorkOrder({ conflictDetected: false, validationChecklist: conflictFreeChecklist }));

    expect(screen.getByText('No Schedule Conflicts')).toBeInTheDocument();
    expect(screen.queryByText('Schedule Conflicts')).not.toBeInTheDocument();
  });

  it('falls back to conflictDetected when the audit checklist is missing', async () => {
    await openAuditTab(baseWorkOrder({ conflictDetected: true, validationChecklist: null }));

    expect(screen.getByText('Schedule Conflicts')).toBeInTheDocument();
  });

  it('treats a legacy checklist without scheduleConflictNone as a conflict when conflictDetected is true', async () => {
    await openAuditTab(baseWorkOrder({ conflictDetected: true, validationChecklist: {} }));

    expect(screen.getByText('Schedule Conflicts')).toBeInTheDocument();
  });
});
