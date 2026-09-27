import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../../services/api';
import {
  PageHeader,
  Card,
  Button,
  Input,
  LoadingState
} from '../../components/SharedUI';

/**
 * SubmitRequestPage — /submit-request  (Requester role)
 *
 * Lets a resident submit a new maintenance request.
 * - Category dropdown is populated from GET /api/issue-categories
 * - Location dropdown is populated from GET /api/locations
 * - Validates client-side before hitting the API
 * - Shows inline errors, a loading state while submitting, and a success banner
 */
export const SubmitRequestPage = () => {
  const navigate = useNavigate();

  // ── Remote data for dropdowns ─────────────────────────────────────────────
  //removed category state since it's now AI-determined
  const [locations,  setLocations]  = useState([]);
  const [assets,     setAssets]     = useState([]);
  const [dropLoading, setDropLoading] = useState(true);
  const [dropError,   setDropError]   = useState('');

  useEffect(() => {
    Promise.all([
      api.get('/locations'),
      api.get('/assets')
    ])
      .then(([locRes, assetRes]) => {
        if (locRes?.success)   setLocations(locRes.data   || []);
        if (assetRes?.success) setAssets(assetRes.data    || []);
      })
      .catch(() => setDropError('Failed to load form options. Please refresh.'))
      .finally(() => setDropLoading(false));
  }, []);

  // ── Form state ────────────────────────────────────────────────────────────
  const [form, setForm] = useState({
    title:       '',
    description: '',
    locationId:  '',
    //removed categoryId from form state since it's now AI-determined
    assetId:     ''
  });

  const [errors,     setErrors]     = useState({});
  const [submitting, setSubmitting] = useState(false);
  const [apiError,   setApiError]   = useState('');
  const [success,    setSuccess]    = useState(false);

  // ── Cascading Location Logic ────────────────────────────────────────────────
  const [selectedBuilding, setSelectedBuilding] = useState('');
  const [selectedFloor, setSelectedFloor] = useState('');

  const buildings = [...new Set(locations.map(l => l.building).filter(Boolean))].sort();
  const floors = selectedBuilding
    ? [...new Set(locations.filter(l => l.building === selectedBuilding).map(l => l.floor).filter(Boolean))].sort()
    : [];
  const rooms = (selectedBuilding && selectedFloor)
    ? locations.filter(l => l.building === selectedBuilding && l.floor === selectedFloor)
    : [];

  // When building changes, reset floor and room
  const handleBuildingChange = (e) => {
    setSelectedBuilding(e.target.value);
    setSelectedFloor('');
    setForm(prev => ({ ...prev, locationId: '' }));
  };

  // When floor changes, reset room
  const handleFloorChange = (e) => {
    setSelectedFloor(e.target.value);
    setForm(prev => ({ ...prev, locationId: '' }));
  };

  // ── Client-side validation ────────────────────────────────────────────────
  const validate = () => {
    const errs = {};
    if (!form.title.trim())
      errs.title = 'Title is required.';
    else if (form.title.trim().length > 200)
      errs.title = 'Title must not exceed 200 characters.';

    if (!form.description.trim())
      errs.description = 'Description is required.';
    else if (form.description.trim().length < 20)
      errs.description = 'Description must be at least 20 characters.';

    if (!form.locationId)
      errs.locationId = 'Please select a location.';

    return errs;
  };

  // ── Handlers ──────────────────────────────────────────────────────────────
  const handleChange = (e) => {
    const { name, value } = e.target;
    setForm(prev => ({ ...prev, [name]: value }));
    // Clear field error on change
    if (errors[name]) setErrors(prev => ({ ...prev, [name]: '' }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setApiError('');

    const errs = validate();
    if (Object.keys(errs).length > 0) {
      setErrors(errs);
      return;
    }

    const payload = {
      title:       form.title.trim(),
      description: form.description.trim(),
      locationId:  form.locationId,
      // CategoryId is intentionally omitted — it's AI-determined on the backend
      assetId:     form.assetId     || null
    };

    setSubmitting(true);
    try {
      await api.post('/requests', payload);
      setSuccess(true);
      // Navigate to my-requests after a short delay so the user sees the banner
      setTimeout(() => navigate('/my-requests'), 1800);
    } catch (err) {
      setApiError(err.message || 'Failed to submit request. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  // ── Render ────────────────────────────────────────────────────────────────
  if (dropLoading) return <LoadingState message="Loading form..." />;

  return (
    <div>
      <PageHeader
        title="Submit Maintenance Request"
        description="Describe the issue and we'll route it to the right technician."
      />

      {dropError && (
        <div style={{ marginBottom: '1rem', padding: '0.75rem 1rem', borderRadius: '8px', background: 'rgba(255,107,113,0.12)', color: 'var(--danger-color)', fontSize: '0.9rem' }}>
          {dropError}
        </div>
      )}

      {success && (
        <div style={{ marginBottom: '1rem', padding: '0.75rem 1rem', borderRadius: '8px', background: 'rgba(74,222,128,0.12)', color: 'var(--success-color)', fontSize: '0.9rem' }}>
          ✅ Request submitted! Redirecting to your requests…
        </div>
      )}

      <Card>
        <form onSubmit={handleSubmit} noValidate>
          {/* Title */}
          <Input
            label="Title *"
            name="title"
            value={form.title}
            onChange={handleChange}
            placeholder="e.g. Bathroom tap leaking in Unit 305"
            maxLength={200}
            error={errors.title}
            disabled={submitting || success}
          />

          {/* Description */}
          <div className="ff-field" style={{ marginTop: '1rem' }}>
            <label className="ff-label">Description *</label>
            <textarea
              name="description"
              value={form.description}
              onChange={handleChange}
              placeholder="Describe the issue in detail (at least 20 characters)…"
              rows={4}
              disabled={submitting || success}
              className={`ff-input${errors.description ? ' ff-input-error' : ''}`}
              style={{ resize: 'vertical', fontFamily: 'inherit' }}
            />
            {errors.description && (
              <span className="ff-error-text">{errors.description}</span>
            )}
          </div>

          {/* Location */}
          <div className="ff-field" style={{ marginTop: '1rem' }}>
            <label className="ff-label">Location *</label>
            <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap' }}>
              <select
                value={selectedBuilding}
                onChange={handleBuildingChange}
                disabled={submitting || success}
                className="ff-input"
                style={{ flex: 1, minWidth: '120px' }}
              >
                <option value="">— Building —</option>
                {buildings.map(b => (
                  <option key={b} value={b}>{b}</option>
                ))}
              </select>

              <select
                value={selectedFloor}
                onChange={handleFloorChange}
                disabled={!selectedBuilding || submitting || success}
                className="ff-input"
                style={{ flex: 1, minWidth: '120px' }}
              >
                <option value="">— Floor —</option>
                {floors.map(f => (
                  <option key={f} value={f}>{f}</option>
                ))}
              </select>

              <select
                name="locationId"
                value={form.locationId}
                onChange={handleChange}
                disabled={!selectedFloor || submitting || success}
                className={`ff-input${errors.locationId ? ' ff-input-error' : ''}`}
                style={{ flex: 2, minWidth: '200px' }}
              >
                <option value="">— Room / Unit —</option>
                {rooms.map(r => (
                  <option key={r.id} value={r.id}>{r.room || r.name}</option>
                ))}
              </select>
            </div>
            {errors.locationId && (
              <span className="ff-error-text" style={{ marginTop: '0.25rem', display: 'block' }}>{errors.locationId}</span>
            )}
          </div>

          {/* What's affected (optional) */}
          <div className="ff-field" style={{ marginTop: '1rem' }}>
            <label className="ff-label">What is affected? <span style={{ color: 'var(--text-secondary)', fontWeight: 400 }}>(optional)</span></label>
            <select
              name="assetId"
              value={form.assetId}
              onChange={handleChange}
              disabled={submitting || success}
              className="ff-input"
            >
              <option value="">— Not sure / general area —</option>
              {assets.map(a => (
                <option key={a.id} value={a.id}>{a.name}</option>
              ))}
            </select>
          </div>

          {/* API error */}
          {apiError && (
            <div style={{ marginTop: '1rem', padding: '0.7rem', borderRadius: '8px', background: 'rgba(255,107,113,0.12)', color: 'var(--danger-color)', fontSize: '0.88rem' }}>
              {apiError}
            </div>
          )}

          {/* Submit */}
          <div style={{ marginTop: '1.5rem', display: 'flex', gap: '0.75rem' }}>
            <Button type="submit" disabled={submitting || success}>
              {submitting ? 'Submitting…' : 'Submit Request'}
            </Button>
            <Button
              type="button"
              variant="ghost"
              onClick={() => navigate('/my-requests')}
              disabled={submitting}
            >
              Cancel
            </Button>
          </div>
        </form>
      </Card>
    </div>
  );
};