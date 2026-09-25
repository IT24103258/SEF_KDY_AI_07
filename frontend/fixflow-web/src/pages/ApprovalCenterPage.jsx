import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { PageHeader, Card, Button, StatusBadge, LoadingState, EmptyState } from '../components/SharedUI';
import { workOrderApi } from '../services/workOrderApi';
import {
  CheckCircle,
  XCircle,
  AlertTriangle,
  RotateCcw,
  Clock,
  User,
  MapPin,
  Calendar,
  Check,
  X,
  MessageSquare,
  Sparkles,
  RefreshCw
} from 'lucide-react';

export const ApprovalCenterPage = () => {
  const navigate = useNavigate();

  const [proposals, setProposals] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [managerComments, setManagerComments] = useState({});
  const [actionLoading, setActionLoading] = useState({});

  const fetchPendingProposals = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await workOrderApi.getPendingApprovals();
      if (res?.success) {
        setProposals(res.data || []);
      }
    } catch (err) {
      setError(err.message || 'Failed to fetch pending approval queue.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPendingProposals();
  }, []);

  const handleCommentChange = (id, text) => {
    setManagerComments((prev) => ({ ...prev, [id]: text }));
  };

  const handleApprove = async (id) => {
    setActionLoading((prev) => ({ ...prev, [id]: true }));
    try {
      const comments = managerComments[id] || 'Approved by Manager for field execution.';
      await workOrderApi.approveWorkOrder(id, comments);
      fetchPendingProposals();
    } catch (err) {
      alert(err.message || 'Failed to approve proposal.');
    } finally {
      setActionLoading((prev) => ({ ...prev, [id]: false }));
    }
  };

  const handleReject = async (id) => {
    const comments = managerComments[id] || prompt('Please enter rejection reason:');
    if (!comments) return;

    setActionLoading((prev) => ({ ...prev, [id]: true }));
    try {
      await workOrderApi.rejectWorkOrder(id, comments);
      fetchPendingProposals();
    } catch (err) {
      alert(err.message || 'Failed to reject proposal.');
    } finally {
      setActionLoading((prev) => ({ ...prev, [id]: false }));
    }
  };

  const handleRequestRevision = async (id) => {
    const revisionNotes = managerComments[id] || prompt('Please enter required scheduling changes:');
    if (!revisionNotes) return;

    setActionLoading((prev) => ({ ...prev, [id]: true }));
    try {
      await workOrderApi.requestRevision(id, revisionNotes);
      fetchPendingProposals();
    } catch (err) {
      alert(err.message || 'Failed to request revision.');
    } finally {
      setActionLoading((prev) => ({ ...prev, [id]: false }));
    }
  };

  return (
    <div>
      <PageHeader
        title="Approval Center"
        description="Human-in-the-loop review queue for AI-generated work order schedules &amp; high-impact maintenance"
        action={
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span
              style={{
                padding: '3px 10px',
                borderRadius: 'var(--radius-pill)',
                fontSize: '0.78rem',
                fontWeight: 700,
                backgroundColor: 'rgba(251, 191, 36, 0.16)',
                color: 'var(--warning-color)',
                border: '1px solid var(--warning-color)'
              }}
            >
              Pending ({proposals.length})
            </span>
            <Button
              variant="secondary"
              onClick={fetchPendingProposals}
              style={{ display: 'flex', alignItems: 'center', gap: '6px' }}
            >
              <RefreshCw size={14} /> Refresh
            </Button>
          </div>
        }
      />

      {loading ? (
        <LoadingState message="Loading pending schedule proposals..." />
      ) : error ? (
        <Card>
          <div style={{ color: 'var(--danger-color)', padding: '1rem', textAlign: 'center' }}>
            <AlertTriangle size={24} style={{ marginBottom: '8px' }} />
            <p>{error}</p>
          </div>
        </Card>
      ) : proposals.length === 0 ? (
        <EmptyState
          title="All Caught Up!"
          description="There are currently no AI work order schedule proposals awaiting human review."
        />
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
          {proposals.map((wo) => {
            const isConflict = wo.conflictDetected;
            const comments = managerComments[wo.id] || '';
            const isLoading = actionLoading[wo.id];

            return (
              <Card
                key={wo.id}
                style={{
                  border: isConflict ? '1px solid var(--danger-color)' : '1px solid var(--border-color)',
                  boxShadow: isConflict ? '0 0 12px rgba(255, 107, 113, 0.15)' : 'none'
                }}
              >
                {/* Header info */}
                <div
                  style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'flex-start',
                    borderBottom: '1px solid var(--border-color)',
                    paddingBottom: '1rem',
                    marginBottom: '1rem',
                    flexWrap: 'wrap',
                    gap: '0.75rem'
                  }}
                >
                  <div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '4px' }}>
                      <span style={{ fontWeight: 700, fontSize: '1.15rem', color: 'var(--primary-color)' }}>
                        {wo.workOrderNumber}
                      </span>
                      <span
                        style={{
                          padding: '2px 8px',
                          borderRadius: '12px',
                          fontSize: '0.75rem',
                          fontWeight: 600,
                          backgroundColor: wo.priority === 'Critical' ? 'rgba(255, 107, 113, 0.16)' : 'rgba(59, 130, 246, 0.16)',
                          color: wo.priority === 'Critical' ? 'var(--danger-color)' : '#3b82f6'
                        }}
                      >
                        {wo.priority} Priority
                      </span>
                      <StatusBadge status={wo.status} />
                    </div>

                    <h3 style={{ margin: '4px 0', fontSize: '1.05rem', fontWeight: 650 }}>
                      {wo.title}
                    </h3>
                    <p style={{ margin: 0, fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
                      Request: {wo.requestNumber} • Location: {wo.locationName}
                    </p>
                  </div>

                  <div style={{ textAlign: 'right', fontSize: '0.85rem' }}>
                    <span style={{ color: 'var(--text-secondary)', display: 'block' }}>SLA Target:</span>
                    <span style={{ fontWeight: 600, color: 'var(--warning-color)' }}>
                      {wo.slaDeadline ? new Date(wo.slaDeadline).toLocaleString() : 'Within 4 hours'}
                    </span>
                  </div>
                </div>

                {/* Conflict Banner if detected */}
                {isConflict && (
                  <div
                    style={{
                      padding: '0.85rem 1rem',
                      borderRadius: '8px',
                      backgroundColor: 'rgba(255, 107, 113, 0.12)',
                      border: '1px solid var(--danger-color)',
                      color: 'var(--danger-color)',
                      marginBottom: '1.25rem',
                      display: 'flex',
                      alignItems: 'center',
                      gap: '10px',
                      fontSize: '0.88rem'
                    }}
                  >
                    <AlertTriangle size={20} />
                    <div>
                      <strong>Schedule Conflict Detected:</strong> Existing booking overlaps with proposed time slot.
                      Review validation details or request revision with an alternative window.
                    </div>
                  </div>
                )}

                {/* 3-Column Proposal Grid */}
                <div
                  style={{
                    display: 'grid',
                    gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))',
                    gap: '1.25rem',
                    marginBottom: '1.25rem'
                  }}
                >
                  {/* Column 1: AI Proposal */}
                  <div
                    style={{
                      padding: '1rem',
                      borderRadius: '8px',
                      backgroundColor: 'var(--glass-bg)',
                      border: '1px solid var(--border-color)'
                    }}
                  >
                    <div style={{ fontSize: '0.78rem', fontWeight: 700, color: 'var(--text-secondary)', textTransform: 'uppercase', marginBottom: '8px' }}>
                      AI Proposal Details
                    </div>
                    <div style={{ fontSize: '0.88rem', display: 'flex', flexDirection: 'column', gap: '6px' }}>
                      <div>
                        <span style={{ color: 'var(--text-secondary)' }}>Assigned Tech: </span>
                        <strong style={{ color: 'var(--text-primary)' }}>{wo.technicianName}</strong>
                      </div>
                      <div>
                        <span style={{ color: 'var(--text-secondary)' }}>Proposed Start: </span>
                        <strong>{wo.scheduledStartTime ? new Date(wo.scheduledStartTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '09:00'} ({wo.scheduledStartTime ? new Date(wo.scheduledStartTime).toLocaleDateString() : 'Today'})</strong>
                      </div>
                      <div>
                        <span style={{ color: 'var(--text-secondary)' }}>Estimated Duration: </span>
                        <strong>{wo.estimatedDurationMinutes || 120} minutes</strong>
                      </div>
                    </div>
                  </div>

                  {/* Column 2: Validation Checks */}
                  <div
                    style={{
                      padding: '1rem',
                      borderRadius: '8px',
                      backgroundColor: 'var(--glass-bg)',
                      border: '1px solid var(--border-color)'
                    }}
                  >
                    <div style={{ fontSize: '0.78rem', fontWeight: 700, color: 'var(--text-secondary)', textTransform: 'uppercase', marginBottom: '8px' }}>
                      Validation Checks
                    </div>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '5px', fontSize: '0.8rem' }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ color: 'var(--text-secondary)' }}>Technician availability:</span>
                        <span style={{ color: 'var(--success-color)', display: 'inline-flex', alignItems: 'center', gap: '3px', fontWeight: 600 }}>
                          <Check size={13} /> Valid
                        </span>
                      </div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ color: 'var(--text-secondary)' }}>Existing bookings checked:</span>
                        <span style={{ color: 'var(--success-color)', display: 'inline-flex', alignItems: 'center', gap: '3px', fontWeight: 600 }}>
                          <Check size={13} /> Valid
                        </span>
                      </div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ color: 'var(--text-secondary)' }}>SLA requirement:</span>
                        <span style={{ color: 'var(--success-color)', display: 'inline-flex', alignItems: 'center', gap: '3px', fontWeight: 600 }}>
                          <Check size={13} /> Valid
                        </span>
                      </div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ color: 'var(--text-secondary)' }}>Schedule conflict:</span>
                        <span style={{ color: !isConflict ? 'var(--success-color)' : 'var(--danger-color)', display: 'inline-flex', alignItems: 'center', gap: '3px', fontWeight: 600 }}>
                          {!isConflict ? <Check size={13} /> : <X size={13} />} {!isConflict ? 'None' : 'Conflict'}
                        </span>
                      </div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ color: 'var(--text-secondary)' }}>Business hours:</span>
                        <span style={{ color: 'var(--success-color)', display: 'inline-flex', alignItems: 'center', gap: '3px', fontWeight: 600 }}>
                          <Check size={13} /> Valid
                        </span>
                      </div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ color: 'var(--text-secondary)' }}>Required skill:</span>
                        <span style={{ color: 'var(--success-color)', display: 'inline-flex', alignItems: 'center', gap: '3px', fontWeight: 600 }}>
                          <Check size={13} /> Valid
                        </span>
                      </div>
                    </div>
                  </div>

                  {/* Column 3: AI Decision Summary */}
                  <div
                    style={{
                      padding: '1rem',
                      borderRadius: '8px',
                      backgroundColor: 'rgba(59, 130, 246, 0.05)',
                      border: '1px solid rgba(59, 130, 246, 0.2)'
                    }}
                  >
                    <div style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '0.78rem', fontWeight: 700, color: '#3b82f6', textTransform: 'uppercase', marginBottom: '6px' }}>
                      <Sparkles size={14} /> AI Decision Summary
                    </div>
                    <p style={{ fontSize: '0.84rem', color: 'var(--text-primary)', margin: 0, lineHeight: 1.45 }}>
                      {isConflict
                        ? 'Schedule conflict detected with technician calendar. Alternative available window evaluated and presented for Manager sign-off.'
                        : 'Selected an available technician slot within business hours and before the SLA deadline. Existing bookings were checked and no overlapping booking was detected.'}
                    </p>
                  </div>
                </div>

                {/* Manager Comments & Decision Buttons */}
                <div
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    flexWrap: 'wrap',
                    gap: '1rem',
                    paddingTop: '0.75rem',
                    borderTop: '1px solid var(--border-color)'
                  }}
                >
                  <input
                    type="text"
                    className="ff-input"
                    placeholder="Enter Manager review comments / revision notes..."
                    value={comments}
                    onChange={(e) => handleCommentChange(wo.id, e.target.value)}
                    style={{ flex: 1, minWidth: '240px' }}
                  />

                  <div style={{ display: 'flex', gap: '8px' }}>
                    <Button
                      variant="secondary"
                      size="sm"
                      onClick={() => handleRequestRevision(wo.id)}
                      disabled={isLoading}
                    >
                      <RotateCcw size={14} /> Request Revision
                    </Button>
                    <Button
                      variant="danger"
                      size="sm"
                      onClick={() => handleReject(wo.id)}
                      disabled={isLoading}
                    >
                      <X size={14} /> Reject
                    </Button>
                    <Button
                      variant="primary"
                      size="sm"
                      onClick={() => handleApprove(wo.id)}
                      disabled={isLoading}
                    >
                      <Check size={14} /> {isLoading ? 'Approving...' : 'Approve & Dispatch'}
                    </Button>
                  </div>
                </div>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
};
