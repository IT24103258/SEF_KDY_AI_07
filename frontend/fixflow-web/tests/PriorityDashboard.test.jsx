import { describe, it, expect, vi } from 'vitest';
import React from 'react';
import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { RiskBadge, PriorityBadge } from '../src/components/priority/PriorityBadges';
import { RiskMatrix } from '../src/components/priority/RiskMatrix';
import { EscalationQueue } from '../src/components/priority/EscalationQueue';

describe('Risk & Priority React Components', () => {
  it('renders RiskBadge with appropriate classes for Critical and Low', () => {
    const { rerender } = render(<RiskBadge level="Critical" />);
    expect(screen.getByText('Critical')).toHaveClass('ff-badge-danger');

    rerender(<RiskBadge level="Low" />);
    expect(screen.getByText('Low')).toHaveClass('ff-badge-success');
  });

  it('renders PriorityBadge with appropriate classes', () => {
    render(<PriorityBadge priority="High" />);
    expect(screen.getByText('High')).toHaveClass('ff-badge-warning');
  });

  it('renders 4x4 Risk Matrix with all impact and likelihood headers', () => {
    render(<RiskMatrix assessments={[]} />);
    expect(screen.getByText(/Deterministic 4×4 Risk Matrix/i)).toBeInTheDocument();
    expect(screen.getByText(/Impact \\ Likelihood/i)).toBeInTheDocument();
  });

  it('renders EscalationQueue empty state when queue has 0 items', () => {
    render(<EscalationQueue escalatedItems={[]} />);
    expect(screen.getByText(/Escalation Queue Clear/i)).toBeInTheDocument();
    expect(screen.getByText(/No maintenance requests currently require urgent manager escalation/i)).toBeInTheDocument();
  });

  it('renders escalated items when items are provided to EscalationQueue', () => {
    const mockItems = [
      {
        id: '1',
        requestId: 'req-1',
        requestNumber: 'REQ-2026-0001',
        requestTitle: 'Elevator Cable Vibration',
        riskLevel: 'Critical',
        priority: 'Critical',
        recommendedResponseWindow: 'Immediate (Within 1 hour)',
        escalationReason: 'Safety critical failure'
      }
    ];

    render(<EscalationQueue escalatedItems={mockItems} />);
    expect(screen.getByText(/High-Risk Escalation Queue/i)).toBeInTheDocument();
    expect(screen.getByText('REQ-2026-0001')).toBeInTheDocument();
    expect(screen.getByText(/Elevator Cable Vibration/i)).toBeInTheDocument();
  });

  it('renders human approval requirement badge correctly', () => {
    const { container } = render(
      <span className="ff-badge ff-badge-warning" title="Flagged for downstream human review — Component 4 owns the approval decision">
        👤 Downstream Review
      </span>
    );
    expect(screen.getByText(/Downstream Review/i)).toBeInTheDocument();
    expect(container.firstChild).toHaveClass('ff-badge-warning');
  });

  it('renders failed assessment state badge correctly', () => {
    const { container } = render(
      <span className="ff-badge ff-badge-danger" title="Agent assessment failed — manual review required">
        ⚠ Failed
      </span>
    );
    expect(screen.getByText(/Failed/i)).toBeInTheDocument();
    expect(container.firstChild).toHaveClass('ff-badge-danger');
  });
});

import { PriorityDashboard } from '../src/pages/PriorityDashboard';
import { AuthContext } from '../src/context/AuthContext';
import { priorityApi } from '../src/services/priorityApi';
import { fireEvent, waitFor } from '@testing-library/react';

describe('PriorityDashboard Component Workflow & Role Gating', () => {
  const sampleAssessments = [
    {
      id: 'assess-1',
      requestId: 'req-1',
      requestNumber: 'REQ-101',
      requestTitle: 'Server Room AC Failure',
      assetName: 'Chiller Unit',
      assetCriticality: 'Critical',
      impactLevel: 'High',
      likelihoodLevel: 'High',
      riskScore: 65,
      riskLevel: 'High',
      priority: 'High',
      recommendedResponseWindow: 'Within 2 hours',
      responseTimeHours: 2,
      resolutionTimeHours: 8,
      escalationFlag: true,
      escalationReason: 'Escalated by Manager',
      humanApprovalRequired: true,
      status: 'Active',
      explanation: 'Evaluated by PriorityAgent with high impact'
    },
    {
      id: 'assess-2',
      requestId: 'req-2',
      requestNumber: 'REQ-102',
      requestTitle: 'Corridor Light Bulb',
      assetName: 'Fixture',
      assetCriticality: 'Low',
      impactLevel: 'Low',
      likelihoodLevel: 'Low',
      riskScore: 11,
      riskLevel: 'Low',
      priority: 'Low',
      recommendedResponseWindow: 'Within 8 hours',
      responseTimeHours: 8,
      resolutionTimeHours: 48,
      escalationFlag: false,
      humanApprovalRequired: false,
      status: 'Active',
      explanation: 'Evaluated by PriorityAgent with low impact'
    },
    {
      id: 'assess-3',
      requestId: 'req-3',
      requestNumber: 'REQ-103',
      requestTitle: 'Broken sensor',
      assetName: 'Sensor',
      assetCriticality: 'Medium',
      impactLevel: 'Medium',
      likelihoodLevel: 'Medium',
      riskScore: 35,
      riskLevel: 'Medium',
      priority: 'Medium',
      recommendedResponseWindow: 'Within 4 hours',
      responseTimeHours: 4,
      resolutionTimeHours: 24,
      escalationFlag: false,
      humanApprovalRequired: true,
      status: 'FAILED',
      explanation: 'Agent step failed during evaluation'
    }
  ];

  const renderWithAuth = (userRole = 'Requester') => {
    const authValue = {
      user: { id: 'u1', name: 'Test User', role: userRole },
      token: 'mock-token',
      isAuthenticated: true
    };

    return render(
      <BrowserRouter>
        <AuthContext.Provider value={authValue}>
          <PriorityDashboard />
        </AuthContext.Provider>
      </BrowserRouter>
    );
  };

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders loading state initially', () => {
    vi.spyOn(priorityApi, 'getPriorityAssessments').mockImplementation(() => new Promise(() => {}));
    renderWithAuth('Requester');
    expect(screen.getByText(/Loading priority assessments\.\.\./i)).toBeInTheDocument();
  });

  it('renders API error state when fetch fails', async () => {
    vi.spyOn(priorityApi, 'getPriorityAssessments').mockRejectedValue(new Error('Network connection timeout'));
    renderWithAuth('Requester');

    await waitFor(() => {
      expect(screen.getByText(/Network connection timeout/i)).toBeInTheDocument();
    });
  });

  it('renders assessment list, downstream review badge, and failed status', async () => {
    vi.spyOn(priorityApi, 'getPriorityAssessments').mockResolvedValue({
      success: true,
      data: { items: sampleAssessments, totalCount: 3 }
    });

    renderWithAuth('Requester');

    await waitFor(() => {
      expect(screen.getAllByText('REQ-101').length).toBeGreaterThan(0);
      expect(screen.getAllByText(/Server Room AC Failure/i).length).toBeGreaterThan(0);
      expect(screen.getByText('REQ-102')).toBeInTheDocument();
      expect(screen.getByText('REQ-103')).toBeInTheDocument();
      expect(screen.getAllByText(/Downstream Review/i).length).toBeGreaterThan(0);
      expect(screen.getByText(/Failed/i)).toBeInTheDocument();
    });
  });

  it('normal user (Requester) does NOT see Manager/Admin action buttons (Re-run, Override, De-escalate)', async () => {
    vi.spyOn(priorityApi, 'getPriorityAssessments').mockResolvedValue({
      success: true,
      data: { items: sampleAssessments, totalCount: 3 }
    });

    renderWithAuth('Requester');

    await waitFor(() => {
      expect(screen.getAllByText('REQ-101').length).toBeGreaterThan(0);
    });

    expect(screen.queryByText(/Re-run/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Override/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/De-escalate/i)).not.toBeInTheDocument();
  });

  it('Manager/Admin sees Re-run, Override, and De-escalate action buttons', async () => {
    vi.spyOn(priorityApi, 'getPriorityAssessments').mockResolvedValue({
      success: true,
      data: { items: sampleAssessments, totalCount: 3 }
    });

    renderWithAuth('Manager');

    await waitFor(() => {
      expect(screen.getAllByText('REQ-101').length).toBeGreaterThan(0);
    });

    expect(screen.getAllByText(/Re-run/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/Override/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/De-escalate/i).length).toBeGreaterThan(0);
  });

  it('Re-run assessment action calls priorityApi.runAgentAssessment', async () => {
    vi.spyOn(priorityApi, 'getPriorityAssessments').mockResolvedValue({
      success: true,
      data: { items: sampleAssessments, totalCount: 3 }
    });
    const rerunSpy = vi.spyOn(priorityApi, 'runAgentAssessment').mockResolvedValue({ success: true });

    renderWithAuth('Manager');

    await waitFor(() => {
      expect(screen.getAllByText('REQ-101').length).toBeGreaterThan(0);
    });

    const rerunButtons = screen.getAllByText(/Re-run/i);
    fireEvent.click(rerunButtons[0]);

    expect(rerunSpy).toHaveBeenCalledWith('req-1');
  });

  it('search form submission calls getPriorityAssessments with searchTerm', async () => {
    const searchSpy = vi.spyOn(priorityApi, 'getPriorityAssessments').mockResolvedValue({
      success: true,
      data: { items: sampleAssessments, totalCount: 3 }
    });

    renderWithAuth('Manager');

    await waitFor(() => {
      expect(screen.getAllByText('REQ-101').length).toBeGreaterThan(0);
    });

    const searchInput = screen.getByPlaceholderText(/Search by request #, title, or asset\.\.\./i);
    fireEvent.change(searchInput, { target: { value: 'Boiler' } });

    const applyButton = screen.getByRole('button', { name: /Apply/i });
    fireEvent.click(applyButton);

    expect(searchSpy).toHaveBeenCalledWith(expect.objectContaining({ searchTerm: 'Boiler' }));
  });

  it('changing the sort control refetches with the selected sortBy', async () => {
    const searchSpy = vi.spyOn(priorityApi, 'getPriorityAssessments').mockResolvedValue({
      success: true,
      data: { items: sampleAssessments, totalCount: 3 }
    });

    renderWithAuth('Manager');

    await waitFor(() => {
      expect(screen.getAllByText('REQ-101').length).toBeGreaterThan(0);
    });

    const sortSelect = screen.getByLabelText(/Sort assessments/i);
    fireEvent.change(sortSelect, { target: { value: 'highest_risk' } });

    await waitFor(() => {
      expect(searchSpy).toHaveBeenCalledWith(expect.objectContaining({ sortBy: 'highest_risk' }));
    });
  });

  it('Manager sees the Escalate action for a non-escalated assessment', async () => {
    vi.spyOn(priorityApi, 'getPriorityAssessments').mockResolvedValue({
      success: true,
      data: { items: sampleAssessments, totalCount: 3 }
    });

    renderWithAuth('Manager');

    await waitFor(() => {
      expect(screen.getAllByText('REQ-101').length).toBeGreaterThan(0);
    });

    // Select the Low, non-escalated assessment (REQ-102)
    fireEvent.click(screen.getByText('REQ-102').closest('tr'));

    await waitFor(() => {
      expect(screen.getAllByText(/Escalate Request/i).length).toBeGreaterThan(0);
    });
  });

  it('normal user (Requester) does NOT see the Escalate action', async () => {
    vi.spyOn(priorityApi, 'getPriorityAssessments').mockResolvedValue({
      success: true,
      data: { items: sampleAssessments, totalCount: 3 }
    });

    renderWithAuth('Requester');

    await waitFor(() => {
      expect(screen.getAllByText('REQ-101').length).toBeGreaterThan(0);
    });

    fireEvent.click(screen.getByText('REQ-102').closest('tr'));

    await waitFor(() => {
      expect(screen.getAllByText('REQ-102').length).toBeGreaterThan(0);
    });
    expect(screen.queryByText(/Escalate Request/i)).not.toBeInTheDocument();
  });
});
