import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks';
import { api } from '../../services/api';
import {
  PageHeader,
  Card,
  Button,
  StatusBadge,
  LoadingState
} from '../../components/SharedUI';

/**
 * RequestDetailPage — /requests/:id  (owner or staff)
 *
 * Shows full request detail, classification history, and (Manager only)
 * an override control panel.
 * Uses:
 *   GET /api/requests/{id}
 *   POST /api/requests/{id}/classify  (Manager/Admin)
 *   PUT  /api/requests/{id}/classification  (Manager/Admin override)
 */
export const RequestDetailPage = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const isManager = ['Manager', 'Administrator'].includes(user?.role);

  const [request,    setRequest]    = useState(null);
  const [loading,    setLoading]    = useState(true);
  const [error,      setError]      = useState('');

  // Classify action state
  const [classifying,  setClassifying]  = useState(false);
  const [classifyMsg,  setClassifyMsg]  = useState('');
  const [classifyErr,  setClassifyErr]  = useState('');

  // Override form state
  const [showOverride,  setShowOverride]  = useState(false);
  const [overrideForm,  setOverrideForm]  = useState({ category: '', subcategory: '', requiredSkill: '', reason: '' });
  const [overrideErrors, setOverrideErrors] = useState({});
  const [overriding,    setOverriding]    = useState(false);
  const [overrideMsg,   setOverrideMsg]   = useState('');
  const [overrideErr,   setOverrideErr]   = useState('');

  const [categories, setCategories] = useState([]);

  const loadRequest = () => {
    setLoading(true);
    setError('');
    api.get(`/requests/${id}`)
      .then(res => {
        if (res?.success) setRequest(res.data);
      })
      .catch(err => setError(err.message || 'Failed to load request.'))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    loadRequest();
    if (isManager) {
      api.get('/issue-categories')
        .then(res => { if (res?.success) setCategories(res.data || []); })
        .catch(() => {});
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  // ── Classify (Manager/Admin) ───────────────────────────────────────────────
  const handleClassify = async () => {
    setClassifyMsg('');
    setClassifyErr('');
    setClassifying(true);
    try {
      await api.post(`/requests/${id}/classify`, {});
      setClassifyMsg('Classification complete. Refreshing…');
      setTimeout(loadRequest, 800);
    } catch (err) {
      setClassifyErr(err.message || 'Classification failed.');
    } finally {
      setClassifying(false);
    }
  };

  // ── Override (Manager/Admin) ──────────────────────────────────────────────
  const validateOverride = () => {
    const errs = {};
    if (!overrideForm.category.trim()) errs.category = 'Category is required.';
    return errs;
  };

  const handleOverrideChange = (e) => {
    const { name, value } = e.target;
    setOverrideForm(prev => ({ ...prev, [name]: value }));
    if (overrideErrors[name]) setOverrideErrors(prev => ({ ...prev, [name]: '' }));
  };

  const handleOverrideSubmit = async (e) => {
    e.preventDefault();
    setOverrideErr('');
    const errs = validateOverride();
    if (Object.keys(errs).length > 0) { setOverrideErrors(errs); return; }

    setOverriding(true);
    try {
      await api.put(`/requests/${id}/classification`, {
        category:      overrideForm.category.trim(),
        subcategory:   overrideForm.subcategory.trim()   || null,
        requiredSkill: overrideForm.requiredSkill.trim() || null,
        reason:        overrideForm.reason.trim()        || null
      });
      setOverrideMsg('Override recorded successfully. Refreshing…');
      setShowOverride(false);
      setTimeout(loadRequest, 800);
    } catch (err) {
      setOverrideErr(err.message || 'Override failed.');
    } finally {
      setOverriding(false);
    }
  };

  // ── Render ────────────────────────────────────────────────────────────────
  if (loading) return <LoadingState />;
  if (error)   return <div style={{ padding: '2rem', color: 'var(--danger-color)' }}>{error}</div>;
  if (!request) return null;

  const canEdit = request.status === 'Submitted' && request.requesterId === user?.id;

  return (
    <div>
      <PageHeader
        title={request.requestNumber}
        description={request.title}
        action={
          <Button variant="ghost" onClick={() => navigate(-1)}>← Back</Button>
        }
      />

      {/* ── Request Info ── */}
      <Card title="Request Details" style={{ marginBottom: '1rem' }}>
        <dl style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem 2rem', fontSize: '0.9rem' }}>
          <div><dt style={{ color: 'var(--text-secondary)', marginBottom: 2 }}>Status</dt><dd><StatusBadge status={request.status} /></dd></div>
          <div><dt style={{ color: 'var(--text-secondary)', marginBottom: 2 }}>Category</dt><dd>{request.categoryName || '—'}</dd></div>
          <div><dt style={{ color: 'var(--text-secondary)', marginBottom: 2 }}>Requester</dt><dd>{request.requesterName}</dd></div>
          <div><dt style={{ color: 'var(--text-secondary)', marginBottom: 2 }}>Location</dt><dd>{request.locationName || '—'}</dd></div>
          <div><dt style={{ color: 'var(--text-secondary)', marginBottom: 2 }}>Asset</dt><dd>{request.assetName || '—'}</dd></div>
          <div><dt style={{ color: 'var(--text-secondary)', marginBottom: 2 }}>Submitted</dt><dd>{new Date(request.createdAt).toLocaleString()}</dd></div>
        </dl>
        <div style={{ marginTop: '1rem', borderTop: '1px solid var(--border-color)', paddingTop: '1rem' }}>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', marginBottom: 4 }}>Description</p>
          <p style={{ fontSize: '0.93rem', lineHeight: 1.6, whiteSpace: 'pre-wrap' }}>{request.description}</p>
        </div>

        {canEdit && (
          <div style={{ marginTop: '1rem', display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
            <Button size="sm" variant="ghost" onClick={() => navigate(`/requests/${id}/edit`)}>Edit</Button>
            <Button
              size="sm"
              variant="ghost"
              onClick={async () => {
                if (!window.confirm('Delete this request? This cannot be undone.')) return;
                try {
                  await api.delete(`/requests/${id}`);
                  navigate('/my-requests');
                } catch (err) {
                  setError(err.message || 'Failed to delete request.');
                }
              }}
            >
              Delete
            </Button>
          </div>
        )}
      </Card>

      {/* ── Manager Actions ── */}
      {isManager && (
        <Card title="Classification Actions" style={{ marginBottom: '1rem' }}>
          <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap', alignItems: 'center' }}>
            <Button
              size="sm"
              onClick={handleClassify}
              disabled={classifying}
            >
              {classifying ? 'Running AI…' : '🤖 Run AI Classification'}
            </Button>
            <Button
              size="sm"
              variant="ghost"
              onClick={() => { setShowOverride(s => !s); setOverrideErr(''); setOverrideMsg(''); }}
            >
              ✏️ Manual Override
            </Button>
          </div>

          {classifyMsg && <p style={{ marginTop: '0.5rem', color: 'var(--success-color)', fontSize: '0.88rem' }}>{classifyMsg}</p>}
          {classifyErr && <p style={{ marginTop: '0.5rem', color: 'var(--danger-color)',  fontSize: '0.88rem' }}>{classifyErr}</p>}

          {/* Override form */}
          {showOverride && (
            <form onSubmit={handleOverrideSubmit} style={{ marginTop: '1rem', borderTop: '1px solid var(--border-color)', paddingTop: '1rem' }}>
              <p style={{ fontSize: '0.88rem', color: 'var(--text-secondary)', marginBottom: '0.75rem' }}>
                This override will create an audited classification row (IsOverride=true) and advance the request to <strong>Classified</strong>.
              </p>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
                {/* Category */}
                <div className="ff-field">
                  <label className="ff-label">Category *</label>
                  <select name="category" value={overrideForm.category} onChange={handleOverrideChange} className={`ff-input${overrideErrors.category ? ' ff-input-error' : ''}`}>
                    <option value="">— Select —</option>
                    {categories.map(c => <option key={c.id} value={c.name}>{c.name}</option>)}
                  </select>
                  {overrideErrors.category && <span className="ff-error-text">{overrideErrors.category}</span>}
                </div>

                {/* Subcategory */}
                <div className="ff-field">
                  <label className="ff-label">Subcategory</label>
                  <input name="subcategory" className="ff-input" value={overrideForm.subcategory} onChange={handleOverrideChange} placeholder="Optional" />
                </div>

                {/* Required Skill */}
                <div className="ff-field">
                  <label className="ff-label">Required Skill</label>
                  <input name="requiredSkill" className="ff-input" value={overrideForm.requiredSkill} onChange={handleOverrideChange} placeholder="Optional" />
                </div>

                {/* Reason */}
                <div className="ff-field">
                  <label className="ff-label">Reason</label>
                  <input name="reason" className="ff-input" value={overrideForm.reason} onChange={handleOverrideChange} placeholder="Why override?" />
                </div>
              </div>

              {overrideErr && <p style={{ marginTop: '0.5rem', color: 'var(--danger-color)', fontSize: '0.88rem' }}>{overrideErr}</p>}

              <div style={{ marginTop: '1rem', display: 'flex', gap: '0.5rem' }}>
                <Button type="submit" size="sm" disabled={overriding}>
                  {overriding ? 'Saving…' : 'Save Override'}
                </Button>
                <Button type="button" size="sm" variant="ghost" onClick={() => setShowOverride(false)}>Cancel</Button>
              </div>
            </form>
          )}
          {overrideMsg && <p style={{ marginTop: '0.5rem', color: 'var(--success-color)', fontSize: '0.88rem' }}>{overrideMsg}</p>}
        </Card>
      )}

      {/* ── Classification History ── */}
      <Card title="Classification History">
        {request.classifications?.length === 0 ? (
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>
            No classifications yet.{isManager ? ' Use "Run AI Classification" above.' : ''}
          </p>
        ) : (
          request.classifications.map((c, i) => (
            <div
              key={c.id}
              style={{
                padding: '0.85rem',
                borderRadius: '8px',
                background: c.isOverride ? 'rgba(251,191,36,0.08)' : 'var(--glass-bg)',
                border: '1px solid var(--border-color)',
                marginBottom: i < request.classifications.length - 1 ? '0.75rem' : 0
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', flexWrap: 'wrap', gap: '0.5rem', marginBottom: '0.5rem' }}>
                <strong style={{ fontSize: '0.95rem' }}>
                  {c.category}{c.subcategory ? ` / ${c.subcategory}` : ''}
                  {c.isOverride && <span style={{ marginLeft: '0.5rem', fontSize: '0.78rem', color: 'var(--warning-color)', fontWeight: 400 }}>MANAGER OVERRIDE</span>}
                </strong>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
                  {new Date(c.createdAt).toLocaleString()}
                </span>
              </div>
              <div style={{ display: 'flex', gap: '1.5rem', flexWrap: 'wrap', fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
                <span>Confidence: <strong style={{ color: 'var(--text-primary)' }}>{(c.confidenceScore * 100).toFixed(0)}%</strong></span>
                <span>Review needed: <strong style={{ color: c.requiresReview ? 'var(--warning-color)' : 'var(--success-color)' }}>{c.requiresReview ? 'Yes' : 'No'}</strong></span>
                {c.requiredSkill && <span>Skill: <strong style={{ color: 'var(--text-primary)' }}>{c.requiredSkill}</strong></span>}
                {c.overriddenByUserName && <span>By: <strong style={{ color: 'var(--text-primary)' }}>{c.overriddenByUserName}</strong></span>}
              </div>
              {c.reason && (
                <p style={{ marginTop: '0.4rem', fontSize: '0.85rem', fontStyle: 'italic', color: 'var(--text-secondary)' }}>
                  "{c.reason}"
                </p>
              )}
            </div>
          ))
        )}
      </Card>
    </div>
  );
};
