import React, { useEffect, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../../services/api';
import {
  PageHeader,
  Card,
  Button,
  StatusBadge,
  LoadingState,
  EmptyState
} from '../../components/SharedUI';

const STATUS_OPTIONS = [
  'Submitted', 'InReview', 'Classified', 'PriorityAssigned',
  'Matched', 'Scheduled', 'InProgress', 'Completed', 'Cancelled'
];
const TABS = [
  { id: 'all',     label: 'All' },
  { id: 'active',  label: 'Active',  statuses: ['Submitted', 'InReview'] },
  { id: 'history', label: 'History', statuses: ['Classified', 'PriorityAssigned', 'Matched', 'Scheduled', 'InProgress', 'Completed', 'Cancelled'] },
];

/**
 * MyRequestsPage — /my-requests  (Requester role)
 *
 * Shows the logged-in resident's own requests with search, status filter,
 * and pagination.  Uses GET /api/requests/me.
 */
export const MyRequestsPage = () => {
  const navigate = useNavigate();

  const [requests,   setRequests]   = useState([]);
  const [total,      setTotal]      = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading,    setLoading]    = useState(true);
  const [error,      setError]      = useState('');

  const [search,   setSearch]   = useState('');
  const [status,   setStatus]   = useState('');
  const [page,     setPage]     = useState(1);
  const PAGE_SIZE = 10;

  const [activeTab, setActiveTab] = useState('all');

  // When tab changes, override the status filter. User can still narrow further within a tab.
  const handleTabChange = (tabId) => {
    setActiveTab(tabId);
    setStatus('');
    setPage(1);
  };

  // Derive the effective status filter for the API call
  const tab = TABS.find(t => t.id === activeTab);
  const effectiveStatuses = tab?.statuses ?? null; // null means "no tab constraint"

  const fetchRequests = useCallback(() => {
    setLoading(true);
    setError('');

    const params = new URLSearchParams({
      page:     String(page),
      pageSize: String(PAGE_SIZE),
      sortBy:   'CreatedAt',
      sortDesc: 'true'
    });
    if (search.trim()) params.set('search', search.trim());

    // If the user picked a specific status in the dropdown, use that.
    // Otherwise if the active tab constrains statuses, send the first one
    // as a hint (API supports single-status filter; tab filtering is client-side for multi-status).
    if (status) {
      params.set('status', status);
    }

    api.get(`/requests/me?${params}`)
      .then(res => {
        if (res?.success) {
          let items = res.data?.items || [];
          // Client-side tab filter when no specific status is selected
          if (!status && effectiveStatuses) {
            items = items.filter(r => effectiveStatuses.includes(r.status));
          }
          setRequests(items);
          setTotal(res.data?.totalCount || 0);
          setTotalPages(res.data?.totalPages || 1);
        }
      })
      .catch(err => setError(err.message || 'Failed to load your requests.'))
      .finally(() => setLoading(false));
  }, [page, search, status, activeTab, effectiveStatuses]);

  useEffect(() => { fetchRequests(); }, [fetchRequests]);

  const handleSearch = (e) => {
    e.preventDefault();
    setPage(1);
    fetchRequests();
  };

  return (
    <div>
      <PageHeader
        title="My Maintenance Requests"
        description={`${total} total request${total !== 1 ? 's' : ''} submitted`}
        action={
          <Button onClick={() => navigate('/submit-request')}>
            + New Request
          </Button>
        }
      />

      {/* ── Tabs: All / Active / History ── */}
      <div style={{ display: 'flex', gap: '0.25rem', marginBottom: '1rem', borderBottom: '2px solid var(--border-color)' }}>
        {TABS.map(t => (
          <button
            key={t.id}
            onClick={() => handleTabChange(t.id)}
            style={{
              padding: '0.5rem 1.25rem',
              border: 'none',
              background: 'transparent',
              cursor: 'pointer',
              fontSize: '0.9rem',
              fontWeight: activeTab === t.id ? 700 : 400,
              color: activeTab === t.id ? 'var(--primary-color, #2563eb)' : 'var(--text-secondary)',
              borderBottom: activeTab === t.id ? '2px solid var(--primary-color, #2563eb)' : '2px solid transparent',
              marginBottom: '-2px',
              transition: 'all 0.15s',
            }}
          >
            {t.label}
          </button>
        ))}
      </div>

      {/* ── Filters ── */}
      <Card style={{ marginBottom: '1rem' }}>
        <form onSubmit={handleSearch} style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap', alignItems: 'flex-end' }}>
          <div style={{ flex: '1 1 220px' }}>
            <label className="ff-label">Search</label>
            <input
              className="ff-input"
              placeholder="Search by title or number…"
              value={search}
              onChange={e => setSearch(e.target.value)}
            />
          </div>
          <div style={{ flex: '0 1 180px' }}>
            <label className="ff-label">Status</label>
            <select
              className="ff-input"
              value={status}
              onChange={e => { setStatus(e.target.value); setPage(1); }}
            >
              <option value="">All statuses</option>
              {STATUS_OPTIONS.map(s => <option key={s} value={s}>{s}</option>)}
            </select>
          </div>
          <Button type="submit" size="sm">Search</Button>
          <Button type="button" size="sm" variant="ghost" onClick={() => { setSearch(''); setStatus(''); setPage(1); }}>
            Clear
          </Button>
        </form>
      </Card>

      {/* ── Results ── */}
      {loading ? (
        <LoadingState />
      ) : error ? (
        <div style={{ padding: '1rem', color: 'var(--danger-color)', background: 'rgba(255,107,113,0.10)', borderRadius: '8px' }}>{error}</div>
      ) : requests.length === 0 ? (
        <EmptyState
          title="No requests found"
          description="Submit your first maintenance request using the button above."
        />
      ) : (
        <Card>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.9rem' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-secondary)' }}>
                <th style={{ padding: '10px', textAlign: 'left' }}>Number</th>
                <th style={{ padding: '10px', textAlign: 'left' }}>Title</th>
                <th style={{ padding: '10px', textAlign: 'left' }}>Category</th>
                <th style={{ padding: '10px', textAlign: 'left' }}>Status</th>
                <th style={{ padding: '10px', textAlign: 'left' }}>Submitted</th>
                <th style={{ padding: '10px' }}></th>
              </tr>
            </thead>
            <tbody>
              {requests.map(r => (
                <tr key={r.id} style={{ borderBottom: '1px solid var(--border-color)' }}>
                  <td style={{ padding: '10px', fontFamily: 'monospace', fontSize: '0.82rem', color: 'var(--text-secondary)' }}>{r.requestNumber}</td>
                  <td style={{ padding: '10px', fontWeight: 600, maxWidth: '280px' }}>{r.title}</td>
                  <td style={{ padding: '10px' }}>{r.category || <span style={{ color: 'var(--text-secondary)' }}>—</span>}</td>
                  <td style={{ padding: '10px' }}><StatusBadge status={r.status} /></td>
                  <td style={{ padding: '10px', color: 'var(--text-secondary)', whiteSpace: 'nowrap' }}>
                    {new Date(r.createdAt).toLocaleDateString()}
                  </td>
                  <td style={{ padding: '10px', textAlign: 'right' }}>
                    <Button size="sm" variant="ghost" onClick={() => navigate(`/requests/${r.id}`)}>
                      View
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {/* ── Pagination ── */}
          {totalPages > 1 && (
            <div style={{ display: 'flex', justifyContent: 'center', gap: '0.5rem', marginTop: '1rem' }}>
              <Button size="sm" variant="ghost" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>← Prev</Button>
              <span style={{ padding: '0.4rem 0.75rem', fontSize: '0.88rem', color: 'var(--text-secondary)' }}>
                Page {page} of {totalPages}
              </span>
              <Button size="sm" variant="ghost" disabled={page >= totalPages} onClick={() => setPage(p => p + 1)}>Next →</Button>
            </div>
          )}
        </Card>
      )}
    </div>
  );
};
