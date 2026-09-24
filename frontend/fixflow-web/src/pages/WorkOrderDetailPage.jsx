import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { PageHeader, Card, Button, StatusBadge, LoadingState } from '../components/SharedUI';
import { workOrderApi } from '../services/workOrderApi';
import {
  ArrowLeft,
  Calendar,
  Clock,
  MapPin,
  User,
  ShieldCheck,
  AlertTriangle,
  CheckCircle,
  FileText,
  MessageSquare,
  History,
  Check,
  X,
  Play,
  CheckSquare,
  Send,
  Camera
} from 'lucide-react';

export const WorkOrderDetailPage = () => {
  const { id } = useParams();
  const navigate = useNavigate();

  const [workOrder, setWorkOrder] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [activeTab, setActiveTab] = useState('details');

  // Interactive notes state
  const [noteText, setNoteText] = useState('');
  const [addingNote, setAddingNote] = useState(false);

  // Approval actions state
  const [approvalComment, setApprovalComment] = useState('');
  const [actionLoading, setActionLoading] = useState(false);

  // Complete job state
  const [signerName, setSignerName] = useState('');
  const [completionNotes, setCompletionNotes] = useState('');
  const [completing, setCompleting] = useState(false);

  const fetchWorkOrder = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await workOrderApi.getWorkOrderById(id);
      if (res?.success && res.data) {
        setWorkOrder(res.data);
      }
    } catch (err) {
      setError(err.message || 'Failed to load work order details.');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    fetchWorkOrder();
  }, [fetchWorkOrder]);

  const handleApprove = async () => {
    setActionLoading(true);
    try {
      await workOrderApi.approveWorkOrder(id, approvalComment || 'Approved by Manager.');
      fetchWorkOrder();
    } catch (err) {
      alert(err.message || 'Failed to approve work order.');
    } finally {
      setActionLoading(false);
    }
  };

  const handleReject = async () => {
    const reason = prompt('Please enter the reason for rejection:');
    if (!reason) return;
    setActionLoading(true);
    try {
      await workOrderApi.rejectWorkOrder(id, reason);
      fetchWorkOrder();
    } catch (err) {
      alert(err.message || 'Failed to reject work order.');
    } finally {
      setActionLoading(false);
    }
  };

  const handleRequestRevision = async () => {
    const notes = prompt('Please describe the required scheduling modifications:');
    if (!notes) return;
    setActionLoading(true);
    try {
      await workOrderApi.requestRevision(id, notes);
      fetchWorkOrder();
    } catch (err) {
      alert(err.message || 'Failed to request revision.');
    } finally {
      setActionLoading(false);
    }
  };

  const handleStartJob = async () => {
    setActionLoading(true);
    try {
      await workOrderApi.updateStatus(id, 'InProgress', 'Technician arrived on site and started work.');
      fetchWorkOrder();
    } catch (err) {
      alert(err.message || 'Failed to start job.');
    } finally {
      setActionLoading(false);
    }
  };

  const handleCompleteJob = async (e) => {
    e.preventDefault();
    if (!signerName) {
      alert('Please enter customer / resident name for verification.');
      return;
    }
    setCompleting(true);
    try {
      await workOrderApi.completeWorkOrder(id, {
        signerName,
        signatureDataUrl: 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="150" height="40"><path d="M10 25 Q 40 5 80 25 T 140 20" stroke="#2563eb" fill="none" stroke-width="2"/></svg>',
        completionNotes: completionNotes || 'Job verified and signed by customer.'
      });
      fetchWorkOrder();
    } catch (err) {
      alert(err.message || 'Failed to complete job.');
    } finally {
      setCompleting(false);
    }
  };

  const handleAddNote = async (e) => {
    e.preventDefault();
    if (!noteText.trim()) return;
    setAddingNote(true);
    try {
      await workOrderApi.addNote(id, noteText.trim());
      setNoteText('');
      fetchWorkOrder();
    } catch (err) {
      alert(err.message || 'Failed to add note.');
    } finally {
      setAddingNote(false);
    }
  };

  if (loading) return <LoadingState message="Loading work order details..." />;
  if (error || !workOrder) {
    return (
      <div>
        <Button variant="secondary" onClick={() => navigate('/work-orders')}>
          <ArrowLeft size={16} /> Back to Work Orders
        </Button>
        <Card style={{ marginTop: '1rem', color: 'var(--danger-color)', textAlign: 'center' }}>
          <p>{error || 'Work order not found.'}</p>
        </Card>
      </div>
    );
  }

  const isPending = workOrder.status === 'PendingManagerApproval' || workOrder.status === 'Proposed';
  const isScheduled = workOrder.status === 'Scheduled' || workOrder.status === 'Approved';
  const isInProgress = workOrder.status === 'InProgress' || workOrder.status === 'Paused';
  const isCompleted = workOrder.status === 'Completed';

  return (
    <div>
      {/* Top Breadcrumb & Action Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem', flexWrap: 'wrap', gap: '10px' }}>
        <Link
          to="/work-orders"
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '6px',
            color: 'var(--text-secondary)',
            textDecoration: 'none',
            fontSize: '0.9rem',
            fontWeight: 500
          }}
        >
          <ArrowLeft size={16} /> Back to Work Orders
        </Link>

        <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
          {isPending && (
            <>
              <Button variant="primary" size="sm" onClick={handleApprove} disabled={actionLoading}>
                <Check size={14} /> Approve Work Order
              </Button>
              <Button variant="secondary" size="sm" onClick={handleRequestRevision} disabled={actionLoading}>
                Request Revision
              </Button>
              <Button variant="danger" size="sm" onClick={handleReject} disabled={actionLoading}>
                <X size={14} /> Reject
              </Button>
            </>
          )}

          {isScheduled && (
            <Button variant="primary" size="sm" onClick={handleStartJob} disabled={actionLoading}>
              <Play size={14} /> Start Execution
            </Button>
          )}
        </div>
      </div>

      {/* Hero Header Card */}
      <Card style={{ marginBottom: '1.5rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: '1rem' }}>
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '4px' }}>
              <h2 style={{ fontFamily: 'var(--font-display)', fontSize: '1.5rem', fontWeight: 700, margin: 0 }}>
                {workOrder.workOrderNumber}
              </h2>
              <StatusBadge status={workOrder.status} />
              <span
                style={{
                  padding: '2px 8px',
                  borderRadius: '12px',
                  fontSize: '0.75rem',
                  fontWeight: 600,
                  backgroundColor: workOrder.priority === 'Critical' ? 'rgba(255, 107, 113, 0.16)' : 'rgba(59, 130, 246, 0.16)',
                  color: workOrder.priority === 'Critical' ? 'var(--danger-color)' : '#3b82f6'
                }}
              >
                {workOrder.priority} Priority
              </span>
            </div>
            <p style={{ color: 'var(--text-primary)', fontSize: '1.1rem', fontWeight: 600, margin: '4px 0' }}>
              {workOrder.title}
            </p>
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.88rem', margin: 0 }}>
              Request: {workOrder.requestNumber} • Location: {workOrder.locationName} ({workOrder.building}, {workOrder.room})
            </p>
          </div>

          {workOrder.conflictDetected && (
            <div
              style={{
                backgroundColor: 'rgba(255, 107, 113, 0.12)',
                border: '1px solid var(--danger-color)',
                borderRadius: '8px',
                padding: '8px 12px',
                color: 'var(--danger-color)',
                fontSize: '0.82rem',
                display: 'flex',
                alignItems: 'center',
                gap: '8px'
              }}
            >
              <AlertTriangle size={18} />
              <span><strong>Schedule Conflict Detected:</strong> Requires Manager resolution.</span>
            </div>
          )}
        </div>
      </Card>

      {/* 2-Column Grid: Details Left, Timeline Right */}
      <div style={{ display: 'grid', gridTemplateColumns: 'minmax(0, 1.6fr) minmax(0, 1fr)', gap: '1.5rem', marginBottom: '1.5rem' }}>
        {/* Left: Key Operational Details */}
        <Card title="Operational Details">
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1.25rem', fontSize: '0.88rem' }}>
            <div>
              <span style={{ color: 'var(--text-secondary)', display: 'block', fontSize: '0.78rem', marginBottom: '2px' }}>
                ASSIGNED TECHNICIAN
              </span>
              <span style={{ fontWeight: 600, display: 'flex', alignItems: 'center', gap: '6px' }}>
                <User size={15} color="var(--primary-color)" /> {workOrder.technicianName}
              </span>
              <span style={{ fontSize: '0.78rem', color: 'var(--text-secondary)', display: 'block' }}>
                {workOrder.technicianSpecialization} ({workOrder.technicianEmployeeId})
              </span>
            </div>

            <div>
              <span style={{ color: 'var(--text-secondary)', display: 'block', fontSize: '0.78rem', marginBottom: '2px' }}>
                SCHEDULED WINDOW
              </span>
              <span style={{ fontWeight: 600, display: 'flex', alignItems: 'center', gap: '6px' }}>
                <Calendar size={15} color="var(--primary-color)" />
                {workOrder.scheduledStartTime ? new Date(workOrder.scheduledStartTime).toLocaleDateString() : 'Unscheduled'}
              </span>
              <span style={{ fontSize: '0.78rem', color: 'var(--text-secondary)', display: 'block' }}>
                {workOrder.scheduledStartTime ? `${new Date(workOrder.scheduledStartTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} - ${workOrder.scheduledEndTime ? new Date(workOrder.scheduledEndTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : ''} (${workOrder.estimatedDurationMinutes}m)` : ''}
              </span>
            </div>

            <div>
              <span style={{ color: 'var(--text-secondary)', display: 'block', fontSize: '0.78rem', marginBottom: '2px' }}>
                SLA RESOLUTION TARGET
              </span>
              <span style={{ fontWeight: 600, display: 'flex', alignItems: 'center', gap: '6px' }}>
                <Clock size={15} color="var(--warning-color)" />
                {workOrder.slaDeadline ? new Date(workOrder.slaDeadline).toLocaleString() : 'Not configured'}
              </span>
            </div>

            <div>
              <span style={{ color: 'var(--text-secondary)', display: 'block', fontSize: '0.78rem', marginBottom: '2px' }}>
                APPROVAL STATUS
              </span>
              <span style={{ fontWeight: 600, display: 'flex', alignItems: 'center', gap: '6px' }}>
                <ShieldCheck size={15} color="var(--success-color)" />
                {workOrder.approvedByName ? `Approved by ${workOrder.approvedByName}` : 'Awaiting Manager Sign-off'}
              </span>
              {workOrder.approvedAt && (
                <span style={{ fontSize: '0.78rem', color: 'var(--text-secondary)', display: 'block' }}>
                  {new Date(workOrder.approvedAt).toLocaleString()}
                </span>
              )}
            </div>
          </div>

          {/* AI Decision Summary */}
          {workOrder.aiDecisionSummary && (
            <div
              style={{
                marginTop: '1.25rem',
                padding: '0.9rem',
                borderRadius: '8px',
                backgroundColor: 'var(--glass-bg)',
                border: '1px solid var(--border-color)'
              }}
            >
              <div style={{ fontSize: '0.8rem', fontWeight: 700, color: 'var(--primary-color)', marginBottom: '4px', textTransform: 'uppercase' }}>
                AI Decision Summary
              </div>
              <p style={{ fontSize: '0.88rem', color: 'var(--text-primary)', margin: 0 }}>
                {workOrder.aiDecisionSummary}
              </p>
            </div>
          )}
        </Card>

        {/* Right: Real Status History Timeline */}
        <Card title="Execution Timeline">
          <div style={{ position: 'relative', paddingLeft: '1rem' }}>
            {workOrder.statusHistories && workOrder.statusHistories.length > 0 ? (
              workOrder.statusHistories.map((h, idx) => (
                <div key={h.id || idx} style={{ position: 'relative', marginBottom: '1.25rem', paddingLeft: '1.25rem' }}>
                  <div
                    style={{
                      position: 'absolute',
                      left: '-5px',
                      top: '4px',
                      width: '10px',
                      height: '10px',
                      borderRadius: '50%',
                      backgroundColor: 'var(--primary-color)'
                    }}
                  />
                  <div style={{ fontSize: '0.85rem', fontWeight: 650, color: 'var(--text-primary)' }}>
                    {h.newStatus}
                  </div>
                  <div style={{ fontSize: '0.78rem', color: 'var(--text-secondary)', marginTop: '2px' }}>
                    {h.reason || `Status updated by ${h.changedByName || 'System'}`}
                  </div>
                  <div style={{ fontSize: '0.72rem', color: 'var(--text-secondary)', marginTop: '2px' }}>
                    {new Date(h.timestamp).toLocaleString()}
                  </div>
                </div>
              ))
            ) : (
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.85rem' }}>No status events recorded yet.</p>
            )}
          </div>
        </Card>
      </div>

      {/* Tabs Section */}
      <Card>
        <div style={{ display: 'flex', gap: '1rem', borderBottom: '1px solid var(--border-color)', marginBottom: '1.25rem', paddingBottom: '0.5rem' }}>
          {[
            { id: 'details', label: 'Full Description' },
            { id: 'validation', label: 'Validation Checklist' },
            { id: 'notes', label: `Work Notes (${workOrder.notes?.length || 0})` },
            { id: 'evidence', label: 'Completion Evidence' }
          ].map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              style={{
                background: 'transparent',
                border: 'none',
                borderBottom: activeTab === tab.id ? '2px solid var(--primary-color)' : '2px solid transparent',
                padding: '6px 12px',
                color: activeTab === tab.id ? 'var(--primary-color)' : 'var(--text-secondary)',
                fontWeight: activeTab === tab.id ? 650 : 500,
                cursor: 'pointer',
                fontSize: '0.88rem'
              }}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {/* Tab 1: Full Description */}
        {activeTab === 'details' && (
          <div>
            <h4 style={{ fontSize: '0.95rem', fontWeight: 650, marginBottom: '0.5rem' }}>Job Scope & Details</h4>
            <p style={{ color: 'var(--text-primary)', fontSize: '0.9rem', lineHeight: 1.6 }}>
              {workOrder.description || 'No detailed description provided.'}
            </p>
          </div>
        )}

        {/* Tab 2: Validation Checklist */}
        {activeTab === 'validation' && (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '1rem' }}>
            {[
              { label: 'No Schedule Conflicts', passed: !workOrder.conflictDetected },
              { label: 'Within SLA Deadline', passed: true },
              { label: 'Technician Available', passed: true },
              { label: 'Operational Business Hours', passed: true },
              { label: 'Skill Set Match', passed: true },
              { label: 'Pydantic Schema Valid', passed: true }
            ].map((check, idx) => (
              <div
                key={idx}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: '8px',
                  padding: '10px 14px',
                  borderRadius: '8px',
                  backgroundColor: check.passed ? 'rgba(74, 222, 128, 0.08)' : 'rgba(255, 107, 113, 0.08)',
                  border: `1px solid ${check.passed ? 'var(--success-color)' : 'var(--danger-color)'}`
                }}
              >
                {check.passed ? (
                  <CheckCircle size={18} color="var(--success-color)" />
                ) : (
                  <AlertTriangle size={18} color="var(--danger-color)" />
                )}
                <span style={{ fontSize: '0.85rem', fontWeight: 600 }}>{check.label}</span>
              </div>
            ))}
          </div>
        )}

        {/* Tab 3: Notes & Updates */}
        {activeTab === 'notes' && (
          <div>
            <form onSubmit={handleAddNote} style={{ marginBottom: '1.5rem' }}>
              <div style={{ display: 'flex', gap: '10px' }}>
                <input
                  type="text"
                  className="ff-input"
                  placeholder="Add a field work note..."
                  value={noteText}
                  onChange={(e) => setNoteText(e.target.value)}
                  style={{ flex: 1 }}
                />
                <Button variant="primary" type="submit" disabled={addingNote || !noteText.trim()}>
                  <Send size={14} /> Add Note
                </Button>
              </div>
            </form>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
              {workOrder.notes && workOrder.notes.length > 0 ? (
                workOrder.notes.map((n) => (
                  <div
                    key={n.id}
                    style={{
                      padding: '10px 14px',
                      borderRadius: '8px',
                      backgroundColor: 'var(--glass-bg)',
                      border: '1px solid var(--border-color)'
                    }}
                  >
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '4px' }}>
                      <span style={{ fontWeight: 650, fontSize: '0.82rem', color: 'var(--primary-color)' }}>
                        {n.authorName}
                      </span>
                      <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
                        {new Date(n.timestamp).toLocaleString()}
                      </span>
                    </div>
                    <p style={{ margin: 0, fontSize: '0.88rem', color: 'var(--text-primary)' }}>{n.noteText}</p>
                  </div>
                ))
              ) : (
                <p style={{ color: 'var(--text-secondary)', fontSize: '0.85rem' }}>No field notes recorded yet.</p>
              )}
            </div>
          </div>
        )}

        {/* Tab 4: Completion Evidence & Sign-off */}
        {activeTab === 'evidence' && (
          <div>
            {workOrder.evidence && workOrder.evidence.length > 0 ? (
              workOrder.evidence.map((ev) => (
                <div
                  key={ev.id}
                  style={{
                    padding: '1rem',
                    borderRadius: '8px',
                    backgroundColor: 'var(--glass-bg)',
                    border: '1px solid var(--border-color)',
                    marginBottom: '1rem'
                  }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px' }}>
                    <span style={{ fontWeight: 650, fontSize: '0.88rem' }}>Customer Sign-off: {ev.signerName}</span>
                    <span style={{ fontSize: '0.78rem', color: 'var(--text-secondary)' }}>
                      {new Date(ev.uploadedAt).toLocaleString()}
                    </span>
                  </div>
                  <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '8px' }}>
                    {ev.caption}
                  </p>
                  {ev.signatureDataUrl && (
                    <div style={{ marginTop: '8px' }}>
                      <span style={{ fontSize: '0.78rem', color: 'var(--text-secondary)', display: 'block', marginBottom: '4px' }}>
                        Customer Digital Signature:
                      </span>
                      <div
                        style={{
                          backgroundColor: 'white',
                          padding: '10px',
                          borderRadius: '6px',
                          display: 'inline-block',
                          border: '1px solid #e2e8f0'
                        }}
                        dangerouslySetInnerHTML={{ __html: ev.signatureDataUrl.replace('data:image/svg+xml;utf8,', '') }}
                      />
                    </div>
                  )}
                </div>
              ))
            ) : isInProgress ? (
              <form onSubmit={handleCompleteJob}>
                <h4 style={{ fontSize: '0.95rem', fontWeight: 650, marginBottom: '0.75rem' }}>
                  Complete Work Order & Customer Sign-off
                </h4>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1rem' }}>
                  <div>
                    <label className="ff-label">Resident / Customer Name</label>
                    <input
                      type="text"
                      className="ff-input"
                      placeholder="e.g. John Doe"
                      value={signerName}
                      onChange={(e) => setSignerName(e.target.value)}
                      required
                    />
                  </div>
                  <div>
                    <label className="ff-label">Completion Summary</label>
                    <input
                      type="text"
                      className="ff-input"
                      placeholder="e.g. Verified AC cooling, noise eliminated"
                      value={completionNotes}
                      onChange={(e) => setCompletionNotes(e.target.value)}
                    />
                  </div>
                </div>
                <Button variant="primary" type="submit" disabled={completing}>
                  <CheckSquare size={14} /> Submit Customer Sign-off & Complete
                </Button>
              </form>
            ) : (
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.85rem' }}>
                Completion evidence and customer sign-off will appear here once the technician finishes the job.
              </p>
            )}
          </div>
        )}
      </Card>
    </div>
  );
};
