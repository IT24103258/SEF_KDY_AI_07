import React, { useState, useEffect } from 'react';
import { Card, Button, Input } from './SharedUI';
import { workOrderApi } from '../services/workOrderApi';
import { X, Calendar, Clock, AlertTriangle, CheckCircle2, FileText, MapPin, User, Tag } from 'lucide-react';

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

  useEffect(() => {
    if (isOpen) {
      loadDropdowns();
      if (workOrder) {
        setRequestId(workOrder.requestId || '');
        setTitle(workOrder.title || '');
        setDescription(workOrder.description || '');
        setPriority(workOrder.priority || 'Medium');
        setTechnicianId(workOrder.technicianId || '');
        setLocationId(workOrder.locationId || '');
        setScheduledStartTime(workOrder.scheduledStartTime ? workOrder.scheduledStartTime.slice(0, 16) : '');
        setScheduledEndTime(workOrder.scheduledEndTime ? workOrder.scheduledEndTime.slice(0, 16) : '');
        setEstimatedDurationMinutes(workOrder.estimatedDurationMinutes || 60);
      } else {
        resetForm();
      }
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

  const handleRequestChange = (e) => {
    const selectedId = e.target.value;
    setRequestId(selectedId);

    const found = requests.find(r => r.id === selectedId);
    setSelectedRequestObj(found || null);

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

      // Auto-suggest matching Job Title based on category or request title
      if (!title) {
        const catName = (found.categoryName || found.title || '').toLowerCase();
        const matched = JOB_TITLES.find(jt => catName.includes(jt.toLowerCase().split(' ')[0]));
        if (matched) {
          setTitle(matched);
        } else {
          setTitle(JOB_TITLES[0]);
        }
      }
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(null);

    // Client-side validations
    if (!isEditing && (!requestId || requestId.trim() === '')) {
      setError('Please select an existing Maintenance Request.');
      return;
    }

    if (!title || title.trim() === '') {
      setError('Please select a Job Title from the dropdown.');
      return;
    }

    if (!technicianId || technicianId.trim() === '') {
      setError('Please explicitly select an assigned Technician.');
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
        scheduledStartTime: scheduledStartTime ? new Date(scheduledStartTime).toISOString() : null,
        scheduledEndTime: scheduledEndTime ? new Date(scheduledEndTime).toISOString() : null,
        estimatedDurationMinutes: Math.max(1, parseInt(estimatedDurationMinutes, 10) || 60)
      };

      if (isEditing) {
        await workOrderApi.updateWorkOrder(workOrder.id, payload);
      } else {
        await workOrderApi.createWorkOrder(payload);
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
              <select
                className="ff-input"
                value={technicianId}
                onChange={(e) => setTechnicianId(e.target.value)}
                required
              >
                <option value="">-- Select Technician --</option>
                {technicians.map((t) => (
                  <option key={t.id || t.userId} value={t.id || t.userId}>
                    {t.name || `${t.firstName || ''} ${t.lastName || ''}`.trim() || 'Technician'} {t.specialization ? `(${t.specialization})` : ''} {t.employeeId ? `[${t.employeeId}]` : ''}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* Scheduled Start and End */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1.5rem' }}>
            <Input
              label="Scheduled Start"
              type="datetime-local"
              value={scheduledStartTime}
              onChange={(e) => setScheduledStartTime(e.target.value)}
            />

            <Input
              label="Scheduled End"
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
            <Button variant="primary" type="submit" disabled={loading}>
              {loading ? 'Saving...' : isEditing ? 'Update Work Order' : 'Create Work Order'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
