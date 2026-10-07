import React, { useState, useEffect } from 'react';
import { Card, Button, Input } from './SharedUI';
import { workOrderApi } from '../services/workOrderApi';
import { X, Calendar, Clock, AlertTriangle, CheckCircle2, FileText, MapPin, User, Tag, Shield } from 'lucide-react';

const JOB_TITLES = [
  'Electrical Repair',
  'Plumbing Repair',
  'HVAC / Air Conditioning',
  'Elevator / Lift Maintenance',
  'Water Supply & Pump Repair',
  'IT / Network Infrastructure',
  'Civil / Structural Repair',
  'Cleaning & Sanitation',
  'Equipment Maintenance',
  'Safety & Emergency System',
  'General Maintenance'
];

function toDatetimeLocalValue(isoString) {
  if (!isoString) return '';
  const d = new Date(isoString);
  if (isNaN(d.getTime())) return isoString.slice(0, 16);
  const pad = (n) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export const WorkOrderModal = ({ isOpen, onClose, onSaved, workOrder = null }) => {
  const isEditing = !!workOrder;

  const [requestId, setRequestId] = useState('');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState('Medium');
  const [technicianId, setTechnicianId] = useState('');
  const [locationId, setLocationId] = useState('');
  const [scheduledStartTime, setScheduledStartTime] = useState('');
  const [scheduledEndTime, setScheduledEndTime] = useState('');
  const [estimatedDurationMinutes, setEstimatedDurationMinutes] = useState(60);

  const [requests, setRequests] = useState([]);
  const [technicians, setTechnicians] = useState([]);
  const [locations, setLocations] = useState([]);
  const [selectedRequestObj, setSelectedRequestObj] = useState(null);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [assignedTechnician, setAssignedTechnician] = useState(null);
  const [loadingAssignment, setLoadingAssignment] = useState(false);
  const [infoMessage, setInfoMessage] = useState(null);

  useEffect(() => {
    if (!isOpen) return;

    loadDropdowns();
    setAssignedTechnician(null);
    setLoadingAssignment(false);
    setInfoMessage(null);

    if (workOrder) {
      (async () => {
        try {
          const fullRes = await workOrderApi.getWorkOrderById(workOrder.id);
          const full = fullRes?.success && fullRes.data ? fullRes.data : null;
          const src = full || workOrder;

          setRequestId(src.requestId || '');
          setTitle(src.title || '');
          setDescription(src.description || '');
          setPriority(src.priority || 'Medium');
          setTechnicianId(src.technicianId || '');
          setLocationId(src.locationId || '');
          setScheduledStartTime(src.scheduledStartTime ? toDatetimeLocalValue(src.scheduledStartTime) : '');
          setScheduledEndTime(src.scheduledEndTime ? toDatetimeLocalValue(src.scheduledEndTime) : '');
          setEstimatedDurationMinutes(src.estimatedDurationMinutes || 60);

          if (src.technicianId) {
            setAssignedTechnician({
              technicianId: src.technicianId,
              technicianName: src.technicianName || 'Assigned Technician',
              technicianSpecialization: src.technicianSpecialization || '',
              assignmentStatus: 'Assigned'
            });
          }

          if (src.requestId && src.requestNumber) {
            setSelectedRequestObj({
              id: src.requestId,
              requestNumber: src.requestNumber,
              title: src.requestTitle || src.title || '',
              description: src.description || '',
              locationId: src.locationId,
              locationName: src.locationName || '',
              building: src.building || '',
              priority: src.priority || 'Medium',
              status: src.status || ''
            });
          }
        } catch (err) {
          console.error('Failed to load work order details for edit', err);
          setRequestId(workOrder.requestId || '');
          setTitle(workOrder.title || '');
          setDescription(workOrder.description || '');
          setPriority(workOrder.priority || 'Medium');
          setTechnicianId(workOrder.technicianId || '');
          setLocationId(workOrder.locationId || '');
          setScheduledStartTime(workOrder.scheduledStartTime ? toDatetimeLocalValue(workOrder.scheduledStartTime) : '');
          setScheduledEndTime(workOrder.scheduledEndTime ? toDatetimeLocalValue(workOrder.scheduledEndTime) : '');
          setEstimatedDurationMinutes(workOrder.estimatedDurationMinutes || 60);
          if (workOrder.technicianId) {
            setAssignedTechnician({
              technicianId: workOrder.technicianId,
              technicianName: workOrder.technicianName || 'Assigned Technician',
              technicianSpecialization: workOrder.technicianSpecialization || '',
              assignmentStatus: 'Assigned'
            });
          }
        }
      })();
    } else {
      resetForm();
    }
  }, [isOpen, workOrder]);

  const resetForm = () => {
    setRequestId('');
    setTitle('');
    setDescription('');
    setPriority('Medium');
    setTechnicianId('');
    setLocationId('');
    setScheduledStartTime('');
    setScheduledEndTime('');
    setEstimatedDurationMinutes(60);
    setSelectedRequestObj(null);
    setError(null);
    setAssignedTechnician(null);
    setLoadingAssignment(false);
    setInfoMessage(null);
  };

  const loadDropdowns = async () => {
    try {
      const [reqsRes, techsRes, usersRes, locsRes] = await Promise.all([
        workOrderApi.getAvailableRequests().catch(() => null),
        workOrderApi.getTechnicians().catch(() => null),
        workOrderApi.getUsers().catch(() => null),
        workOrderApi.getLocations().catch(() => null)
      ]);

      if (reqsRes?.success && Array.isArray(reqsRes.data)) {
        setRequests(reqsRes.data);
      }

      if (techsRes?.success && Array.isArray(techsRes.data) && techsRes.data.length > 0) {
        setTechnicians(techsRes.data);
      } else if (usersRes?.success && Array.isArray(usersRes.data)) {
        const techUsers = usersRes.data.filter(u => u.role === 'Technician');
        setTechnicians(techUsers.length > 0 ? techUsers : usersRes.data);
      }

      if (locsRes?.success && Array.isArray(locsRes.data)) {
        setLocations(locsRes.data);
      }
    } catch (e) {
      console.error('Failed to load dropdowns', e);
    }
  };

  const handleRequestChange = async (e) => {
    const selectedId = e.target.value;
    setRequestId(selectedId);

    const found = requests.find(r => r.id === selectedId);
    setSelectedRequestObj(found || null);
    setAssignedTechnician(null);
    setInfoMessage(null);

    if (found) {
      if (found.locationId) {
        setLocationId(found.locationId);
      }
      if (found.priority) {
        setPriority(found.priority);
      }
      if (found.description && !description) {
        setDescription(found.description);
      }

      if (!title) {
        const catName = (found.categoryName || found.title || '').toLowerCase();
        const matched = JOB_TITLES.find(jt => catName.includes(jt.toLowerCase().split(' ')[0]));
        if (matched) {
          setTitle(matched);
        } else {
          setTitle(JOB_TITLES[0]);
        }
      }

      if (selectedId) {
        setLoadingAssignment(true);
        try {
          const res = await workOrderApi.getAssignmentForRequest(selectedId);
          if (res?.success && res.data) {
            setAssignedTechnician(res.data);
            setTechnicianId(res.data.technicianId);
          } else {
            setAssignedTechnician(null);
            setTechnicianId('');
          }
        } catch (err) {
          console.error('Failed to load assignment', err);
          setAssignedTechnician(null);
          setTechnicianId('');
        } finally {
          setLoadingAssignment(false);
        }
      }
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(null);
    setInfoMessage(null);

    if (!isEditing && (!requestId || requestId.trim() === '')) {
      setError('Please select an existing Maintenance Request.');
      return;
    }

    if (!title || title.trim() === '') {
      setError('Please select a Job Title from the dropdown.');
      return;
    }

    if (!isEditing && !assignedTechnician) {
      setError('No technician has been assigned to this request. Complete technician assignment before creating the work order.');
      return;
    }

    if (!isEditing && (!technicianId || technicianId.trim() === '')) {
      setError('No technician has been assigned to this request. Complete technician assignment before creating the work order.');
      return;
    }

    if (scheduledStartTime && scheduledEndTime) {
      const start = new Date(scheduledStartTime);
      const end = new Date(scheduledEndTime);
      if (start >= end) {
        setError('Scheduled start time must be earlier than scheduled end time.');
        return;
      }
    }

    setLoading(true);

    try {
      const payload = {
        requestId: requestId,
        technicianId: technicianId,
        locationId: locationId || undefined,
        title: title.trim(),
        description: description.trim(),
        priority: priority,
        scheduledStartTime: scheduledStartTime || null,
        scheduledEndTime: scheduledEndTime || null,
        estimatedDurationMinutes: Math.max(1, parseInt(estimatedDurationMinutes, 10) || 60)
      };

      let result;
      if (isEditing) {
        result = await workOrderApi.updateWorkOrder(workOrder.id, payload);
      } else {
        result = await workOrderApi.createWorkOrder(payload);
      }

      if (result?.data?.aiDecisionSummary && result?.data?.status === 'PendingManagerApproval') {
        setInfoMessage(result.data.aiDecisionSummary);
      }

      if (onSaved) {
        onSaved();
      }
      onClose();
    } catch (err) {
      setError(err.message || 'Failed to save work order.');
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div
      style={{
        position: 'fixed',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        backgroundColor: 'rgba(0, 0, 0, 0.65)',
        backdropFilter: 'blur(4px)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 9999,
        padding: '1rem'
      }}
    >
      <div
        className="ff-card glass-strong"
        style={{
          width: '100%',
          maxWidth: '660px',
          maxHeight: '92vh',
          overflowY: 'auto',
          padding: '1.75rem',
          borderRadius: '12px'
        }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' }}>
          <h3 style={{ fontFamily: 'var(--font-display)', fontSize: '1.25rem', fontWeight: '700' }}>
            {isEditing ? `Edit Work Order #${workOrder.workOrderNumber || ''}` : 'Create New Work Order'}
          </h3>
          <button
            onClick={onClose}
            className="ff-icon-btn"
            style={{ border: 'none', background: 'transparent' }}
          >
            <X size={20} />
          </button>
        </div>

        {error && (
          <div
            style={{
              padding: '0.75rem 1rem',
              backgroundColor: 'rgba(255, 107, 113, 0.15)',
              color: 'var(--danger-color)',
              borderRadius: '8px',
              marginBottom: '1rem',
              fontSize: '0.85rem',
              display: 'flex',
              alignItems: 'center',
              gap: '8px'
            }}
          >
            <AlertTriangle size={16} />
            <span>{error}</span>
          </div>
        )}

        {infoMessage && (
          <div
            style={{
              padding: '0.75rem 1rem',
              backgroundColor: 'rgba(59, 130, 246, 0.12)',
              color: 'var(--primary-color)',
              borderRadius: '8px',
              marginBottom: '1rem',
              fontSize: '0.85rem',
              display: 'flex',
              alignItems: 'flex-start',
              gap: '8px'
            }}
          >
            <Calendar size={16} style={{ marginTop: '2px', flexShrink: 0 }} />
            <span>{infoMessage}</span>
          </div>
        )}

        <form onSubmit={handleSubmit}>
          {/* Request Selection */}
          <div className="ff-field" style={{ marginBottom: '1rem' }}>
            <label className="ff-label" style={{ fontWeight: 600 }}>
              Request <span style={{ color: 'var(--danger-color)' }}>*</span>
            </label>
            <select
              className="ff-input"
              value={requestId}
              onChange={handleRequestChange}
              required
              disabled={isEditing}
              style={isEditing ? { opacity: 0.7, cursor: 'not-allowed' } : {}}
            >
              <option value="">-- Select Existing Maintenance Request --</option>
              {requests.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.requestNumber} — {r.title} ({r.locationName || r.building}) [{r.priority || 'Medium'}]
                </option>
              ))}
            </select>
          </div>

          {/* Selected Request Information Card */}
          {selectedRequestObj && (
            <div
              className="glass"
              style={{
                padding: '0.85rem 1rem',
                borderRadius: '8px',
                marginBottom: '1.25rem',
                fontSize: '0.85rem',
                border: '1px solid var(--border-color)',
                backgroundColor: 'var(--card-bg-hover)'
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '6px' }}>
                <span style={{ fontWeight: 700, color: 'var(--primary-color)', display: 'flex', alignItems: 'center', gap: '5px' }}>
                  <FileText size={14} />
                  {selectedRequestObj.requestNumber}
                </span>
                <span
                  style={{
                    padding: '2px 8px',
                    borderRadius: 'var(--radius-pill)',
                    fontSize: '0.75rem',
                    fontWeight: 600,
                    backgroundColor: 'rgba(59, 130, 246, 0.15)',
                    color: 'var(--primary-color)'
                  }}
                >
                  Status: {selectedRequestObj.status}
                </span>
              </div>
              <div style={{ fontWeight: 600, marginBottom: '4px' }}>{selectedRequestObj.title}</div>
              <div style={{ color: 'var(--text-secondary)', fontSize: '0.8rem', marginBottom: '6px' }}>
                {selectedRequestObj.description}
              </div>
              <div style={{ display: 'flex', gap: '16px', color: 'var(--text-secondary)', fontSize: '0.8rem' }}>
                <span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                  <MapPin size={12} /> {selectedRequestObj.locationName || selectedRequestObj.building}
                </span>
                <span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                  <Tag size={12} /> Priority: <strong>{selectedRequestObj.priority || 'Medium'}</strong>
                </span>
              </div>
            </div>
          )}

          {/* Job Title Dropdown */}
          <div className="ff-field" style={{ marginBottom: '1rem' }}>
            <label className="ff-label" style={{ fontWeight: 600 }}>
              Job Title <span style={{ color: 'var(--danger-color)' }}>*</span>
            </label>
            <select
              className="ff-input"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              required
            >
              <option value="">-- Select Job Title --</option>
              {JOB_TITLES.map((jt) => (
                <option key={jt} value={jt}>
                  {jt}
                </option>
              ))}
            </select>
          </div>

          {/* Description */}
          <div className="ff-field" style={{ marginBottom: '1rem' }}>
            <label className="ff-label">Work Order Description</label>
            <textarea
              className="ff-input"
              rows={3}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Detailed description of scheduled work / instructions..."
              style={{ resize: 'vertical' }}
            />
          </div>

          {/* Priority & Duration */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1rem' }}>
            <div className="ff-field">
              <label className="ff-label">Priority</label>
              <select
                className="ff-input"
                value={priority}
                onChange={(e) => setPriority(e.target.value)}
                disabled={isEditing}
                style={isEditing ? { opacity: 0.7, cursor: 'not-allowed' } : {}}
              >
                <option value="Low">Low</option>
                <option value="Medium">Medium</option>
                <option value="High">High</option>
                <option value="Critical">Critical</option>
              </select>
            </div>

            <div className="ff-field">
              <label className="ff-label">Estimated Duration (mins)</label>
              <input
                type="number"
                min="15"
                step="15"
                className="ff-input"
                value={estimatedDurationMinutes}
                onChange={(e) => setEstimatedDurationMinutes(e.target.value)}
                required
              />
            </div>
          </div>

          {/* Location & Assigned Technician */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1rem' }}>
            <div className="ff-field">
              <label className="ff-label">Location</label>
              <select
                className="ff-input"
                value={locationId}
                onChange={(e) => setLocationId(e.target.value)}
                disabled={isEditing}
                style={isEditing ? { opacity: 0.7, cursor: 'not-allowed' } : {}}
              >
                <option value="">Select Location</option>
                {locations.map((loc) => (
                  <option key={loc.id} value={loc.id}>
                    {loc.name} ({loc.building})
                  </option>
                ))}
              </select>
            </div>

            <div className="ff-field">
              <label className="ff-label" style={{ fontWeight: 600 }}>
                Assigned Technician <span style={{ color: 'var(--danger-color)' }}>*</span>
              </label>
              {loadingAssignment ? (
                <div style={{
                  padding: '0.75rem',
                  borderRadius: '8px',
                  border: '1px solid var(--border-color)',
                  backgroundColor: 'var(--card-bg-hover)',
                  fontSize: '0.85rem',
                  color: 'var(--text-secondary)'
                }}>
                  Loading assignment...
                </div>
              ) : assignedTechnician ? (
                <div style={{
                  padding: '0.75rem',
                  borderRadius: '8px',
                  border: '1px solid rgba(59, 130, 246, 0.3)',
                  backgroundColor: 'rgba(59, 130, 246, 0.08)',
                }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', fontWeight: 600, fontSize: '0.9rem' }}>
                    <User size={16} style={{ color: 'var(--primary-color)' }} />
                    {assignedTechnician.technicianName}
                  </div>
                  {assignedTechnician.technicianSpecialization && (
                    <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', marginTop: '4px' }}>
                      {assignedTechnician.technicianSpecialization}
                    </div>
                  )}
                  <div style={{
                    fontSize: '0.75rem',
                    color: 'var(--primary-color)',
                    marginTop: '6px',
                    display: 'flex',
                    alignItems: 'center',
                    gap: '4px'
                  }}>
                    <Shield size={12} />
                    Assigned by Assignment Agent
                  </div>
                </div>
              ) : requestId ? (
                <div style={{
                  padding: '0.75rem',
                  borderRadius: '8px',
                  border: '1px solid rgba(255, 107, 113, 0.3)',
                  backgroundColor: 'rgba(255, 107, 113, 0.08)',
                  fontSize: '0.85rem',
                  color: 'var(--danger-color)'
                }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                    <AlertTriangle size={14} />
                    No technician assigned to this request.
                  </div>
                  <div style={{ fontSize: '0.8rem', marginTop: '4px', color: 'var(--text-secondary)' }}>
                    Complete technician assignment before creating the work order.
                  </div>
                </div>
              ) : (
                <div style={{
                  padding: '0.75rem',
                  borderRadius: '8px',
                  border: '1px solid var(--border-color)',
                  backgroundColor: 'var(--card-bg-hover)',
                  fontSize: '0.85rem',
                  color: 'var(--text-secondary)'
                }}>
                  Select a request to load the assigned technician.
                </div>
              )}
            </div>
          </div>

          {/* Requested Start and End */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1.5rem' }}>
            <Input
              label="Requested Start"
              type="datetime-local"
              value={scheduledStartTime}
              onChange={(e) => setScheduledStartTime(e.target.value)}
            />

            <Input
              label="Requested End"
              type="datetime-local"
              value={scheduledEndTime}
              onChange={(e) => setScheduledEndTime(e.target.value)}
            />
          </div>

          {/* Modal Action Buttons */}
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
            <Button variant="secondary" type="button" onClick={onClose} disabled={loading}>
              Cancel
            </Button>
            <Button
              variant="primary"
              type="submit"
              disabled={loading || (!isEditing && !assignedTechnician && !!requestId)}
            >
              {loading ? 'Saving...' : isEditing ? 'Update Work Order' : 'Create Work Order'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
