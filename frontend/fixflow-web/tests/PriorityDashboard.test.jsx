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
});
