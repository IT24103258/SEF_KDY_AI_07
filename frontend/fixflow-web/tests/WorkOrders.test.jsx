import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';
import { WorkOrdersPage } from '../src/pages/WorkOrdersPage';
import { ApprovalCenterPage } from '../src/pages/ApprovalCenterPage';
import { CalendarPage } from '../src/pages/CalendarPage';
import { WorkOrderReportsPage } from '../src/pages/WorkOrderReportsPage';
import { WorkOrderModal } from '../src/components/WorkOrderModal';

// Mock workOrderApi
vi.mock('../src/services/workOrderApi', () => ({
  workOrderApi: {
    getWorkOrders: vi.fn().mockResolvedValue({
      success: true,
      data: {
        items: [
          {
            id: 'wo-1',
            workOrderNumber: 'WO-202609-0001',
            title: 'Ceiling Leak Inspection',
            technicianName: 'Senior Technician',
            locationName: 'Tower A Unit 305',
            priority: 'Critical',
            status: 'PendingManagerApproval',
            scheduledStartTime: '2026-09-24T14:00:00Z',
            scheduledEndTime: '2026-09-24T16:00:00Z',
            estimatedDurationMinutes: 120,
            conflictDetected: false
          }
        ],
        totalCount: 1,
        page: 1,
        pageSize: 10
      }
    }),
    getUsers: vi.fn().mockResolvedValue({ success: true, data: [] }),
    getLocations: vi.fn().mockResolvedValue({ success: true, data: [{ id: 'loc-1', name: 'Tower A', building: 'Tower A' }] }),
    getAvailableRequests: vi.fn().mockResolvedValue({
      success: true,
      data: [{ id: 'req-1', requestNumber: 'REQ-2026-0001', title: 'Lobby AC Failure', locationId: 'loc-1', locationName: 'Tower A', priority: 'High' }]
    }),
    getTechnicians: vi.fn().mockResolvedValue({
      success: true,
      data: [{ id: 'tech-1', userId: 'user-1', name: 'Kamal Perera', specialization: 'HVAC', employeeId: 'TECH-001' }]
    }),
    createWorkOrder: vi.fn().mockResolvedValue({ success: true, data: { id: 'wo-new' } }),
    getPendingApprovals: vi.fn().mockResolvedValue({
      success: true,
      data: [
        {
          id: 'wo-1',
          workOrderNumber: 'WO-202609-0001',
          title: 'Ceiling Leak Inspection',
          technicianName: 'Senior Tech',
          locationName: 'Tower A',
          priority: 'Critical',
          status: 'PendingManagerApproval',
          scheduledStartTime: '2026-09-24T14:00:00Z',
          estimatedDurationMinutes: 120,
          conflictDetected: false
        }
      ]
    }),
    getCalendarEvents: vi.fn().mockResolvedValue({
      success: true,
      data: [
        {
          id: 'wo-1',
          workOrderNumber: 'WO-202609-0001',
          title: 'Ceiling Leak Inspection',
          start: '2026-09-24T14:00:00Z',
          end: '2026-09-24T16:00:00Z',
          priority: 'Critical',
          status: 'PendingManagerApproval',
          technicianName: 'Senior Tech',
          conflictDetected: false
        }
      ]
    }),
    getReports: vi.fn().mockResolvedValue({
      success: true,
      data: {
        totalWorkOrders: 6,
        completedCount: 1,
        inProgressCount: 1,
        scheduledCount: 1,
        pendingApprovalCount: 2,
        conflictCount: 1,
        slaComplianceRate: 92.5,
        statusBreakdown: [],
        priorityBreakdown: [],
        technicianWorkloads: []
      }
    })
  }
}));

describe('Component 4 React Frontend Screens', () => {
  it('renders WorkOrdersPage with header and search input', async () => {
    render(
      <BrowserRouter>
        <WorkOrdersPage />
      </BrowserRouter>
    );

    expect(screen.getByRole('heading', { level: 1, name: 'Work Orders' })).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/search work orders/i)).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('WO-202609-0001')).toBeInTheDocument();
      expect(screen.getByText('Ceiling Leak Inspection')).toBeInTheDocument();
    });
  });

  it('renders ApprovalCenterPage with proposal cards', async () => {
    render(
      <BrowserRouter>
        <ApprovalCenterPage />
      </BrowserRouter>
    );

    expect(screen.getByRole('heading', { level: 1, name: 'Approval Center' })).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText(/AI Proposal Details/i)).toBeInTheDocument();
      expect(screen.getByText(/Validation Checks/i)).toBeInTheDocument();
      expect(screen.getByText(/Approve & Dispatch/i)).toBeInTheDocument();
    });
  });

  it('renders CalendarPage with schedule board controls', async () => {
    render(
      <BrowserRouter>
        <CalendarPage />
      </BrowserRouter>
    );

    expect(screen.getByRole('heading', { level: 1, name: 'Schedule Board' })).toBeInTheDocument();
    expect(screen.getByText('Today')).toBeInTheDocument();
    expect(screen.getByText('Week View')).toBeInTheDocument();
  });

  it('renders WorkOrderReportsPage with KPI cards', async () => {
    render(
      <BrowserRouter>
        <WorkOrderReportsPage />
      </BrowserRouter>
    );

    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 1, name: 'Reports & Analytics' })).toBeInTheDocument();
      expect(screen.getAllByText('Completed').length).toBeGreaterThan(0);
      expect(screen.getAllByText('In Progress').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Pending').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Breached').length).toBeGreaterThan(0);
      expect(screen.getByText('Technician Performance')).toBeInTheDocument();
    });
  });

  it('renders WorkOrderModal with request selection and job title dropdown', async () => {
    render(
      <BrowserRouter>
        <WorkOrderModal isOpen={true} onClose={vi.fn()} onSaved={vi.fn()} />
      </BrowserRouter>
    );

    expect(screen.getByText('Create New Work Order')).toBeInTheDocument();
    expect(screen.getAllByText(/Request/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/Job Title/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/Assigned Technician/).length).toBeGreaterThan(0);

    await waitFor(() => {
      expect(screen.getByText(/-- Select Existing Maintenance Request --/)).toBeInTheDocument();
      expect(screen.getByText(/-- Select Job Title --/)).toBeInTheDocument();
    });
  });
});
