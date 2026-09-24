import React, { useState, useEffect } from 'react';
import { Card, Button, Input } from './SharedUI';
import { workOrderApi } from '../services/workOrderApi';
import { X, Calendar, Clock, AlertTriangle } from 'lucide-react';

export const WorkOrderModal = ({ isOpen, onClose, onSaved, workOrder = null }) => {
  const isEditing = !!workOrder;

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState('Medium');
  const [technicianId, setTechnicianId] = useState('');
  const [locationId, setLocationId] = useState('');
  const [scheduledStartTime, setScheduledStartTime] = useState('');
  const [scheduledEndTime, setScheduledEndTime] = useState('');
  const [estimatedDurationMinutes, setEstimatedDurationMinutes] = useState(60);

  const [technicians, setTechnicians] = useState([]);
  const [locations, setLocations] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (isOpen) {
      loadDropdowns();
      if (workOrder) {
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
    setTitle('');
    setDescription('');
    setPriority('Medium');
    setTechnicianId('');
    setLocationId('');
    setScheduledStartTime('');
    setScheduledEndTime('');
    setEstimatedDurationMinutes(60);
    setError(null);
  };

  const loadDropdowns = async () => {
    try {
      const [usersRes, locsRes] = await Promise.all([
        workOrderApi.getUsers(),
        workOrderApi.getLocations()
      ]);
      if (usersRes?.success) {
        // Filter users by role or show all users
        setTechnicians(usersRes.data || []);
      }
      if (locsRes?.success) {
        setLocations(locsRes.data || []);
      }
    } catch (e) {
      console.error('Failed to load dropdowns', e);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const payload = {
        title,
        description,
        priority,
        technicianId: technicianId || undefined,
        locationId: locationId || undefined,
        scheduledStartTime: scheduledStartTime ? new Date(scheduledStartTime).toISOString() : undefined,
        scheduledEndTime: scheduledEndTime ? new Date(scheduledEndTime).toISOString() : undefined,
        estimatedDurationMinutes: Number(estimatedDurationMinutes) || 60
      };

      if (isEditing) {
        await workOrderApi.updateWorkOrder(workOrder.id, payload);
      } else {
        await workOrderApi.createWorkOrder(payload);
      }

      onSaved();
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
          maxWidth: '620px',
          maxHeight: '90vh',
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
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <Input
            label="Job Title"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            required
            placeholder="e.g. Lobby HVAC Inspection"
          />

          <div className="ff-field" style={{ marginBottom: '1rem' }}>
            <label className="ff-label">Description</label>
            <textarea
              className="ff-input"
              rows={3}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Detailed description of maintenance required..."
              style={{ resize: 'vertical' }}
            />
          </div>

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
              />
            </div>
          </div>

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
              <label className="ff-label">Assigned Technician</label>
              <select
                className="ff-input"
                value={technicianId}
                onChange={(e) => setTechnicianId(e.target.value)}
              >
                <option value="">Select Technician</option>
                {technicians.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.firstName} {t.lastName} ({t.role || 'Staff'})
                  </option>
                ))}
              </select>
            </div>
          </div>

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
