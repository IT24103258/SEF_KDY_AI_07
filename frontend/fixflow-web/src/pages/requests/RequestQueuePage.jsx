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

const SORT_OPTIONS = [
  { value: 'CreatedAt|true',  label: 'Newest first' },
  { value: 'CreatedAt|false', label: 'Oldest first' },
  { value: 'Title|false',     label: 'Title A–Z' },
  { value: 'Status|false',    label: 'Status' }
];

/**
 * RequestQueuePage — /request-queue  (Manager / Administrator)
 *
 * Full queue view of all maintenance requests with search, filter by
 * status + category, sort, and pagination.  Uses GET /api/requests.
 */
export const RequestQueuePage = () => {
  const navigate = useNavigate();

  const [requests,   setRequests]   = useState([]);
  const [total,      setTotal]      = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading,    setLoading]    = useState(true);
  const [error,      setError]      = useState('');

  const [categories, setCategories] = useState([]);

  const [search,   setSearch]   = useState('');
  const [status,   setStatus]   = useState('');
  const [category, setCategory] = useState('');
  const [sort,     setSort]     = useState('CreatedAt|true');
  const [page,     setPage]     = useState(1);
  const PAGE_SIZE = 20;

  // Load category list once for the filter dropdown
  useEffect(() => {
    api.get('/issue-categories')
      .then(res => { if (res?.success) setCategories(res.data || []); })
      .catch(() => {});
  }, []);

  const fetchRequests = useCallback(() => {
    setLoading(true);
    setError('');

    const [sortBy, sortDesc] = sort.split('|');
    const params = new URLSearchParams({
      page:     String(page),
      pageSize: String(PAGE_SIZE),
      sortBy,
      sortDesc
    });
    if (search.trim()) params.set('search', search.trim());
    if (status)        params.set('status', status);
    if (category)      params.set('category', category);

    api.get(`/requests?${params}`)
      .then(res => {
        if (res?.success) {
          setRequests(res.data?.items   || []);
          setTotal(res.data?.totalCount || 0);
          setTotalPages(res.data?.totalPages || 1);
        }
      })
      .catch(err => setError(err.message || 'Failed to load requests.'))
      .finally(() => setLoading(false));
  }, [page, search, status, category, sort]);

  useEffect(() => { fetchRequests(); }, [fetchRequests]);

  const handleSearch = (e) => {
    e.preventDefault();
    setPage(1);
    fetchRequests();
  };

  return (
    <div>
      <PageHeader
        title="Request Queue"
        description={`${total} total request${total !== 1 ? 's' : ''} in the system`}
      />

      {/* ── Filters ── */}
      <Card style={{ marginBottom: '1rem' }}>
        <form onSubmit={handleSearch} style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap', alignItems: 'flex-end' }}>
          <div style={{ flex: '1 1 220px' }}>
            <label className="ff-label">Search</label>
            <input
              className="ff-input"
              placeholder="Title, description, or number…"
              value={search}
              onChange={e => setSearch(e.target.value)}
            />
          </div>
          <div style={{ flex: '0 1 160px' }}>
            <label className="ff-label">Status</label>
            <select className="ff-input" value={status} onChange={e => { setStatus(e.target.value); setPage(1); }}>
              <option value="">All statuses</option>
              {STATUS_OPTIONS.map(s => <option key={s} value={s}>{s}</option>)}
            </select>
          </div>
          <div style={{ flex: '0 1 160px' }}>
            <label className="ff-label">Category</label>
            <select className="ff-input" value={category} onChange={e => { setCategory(e.target.value); setPage(1); }}>
              <option value="">All categories</option>
              {categories.map(c => <option key={c.id} value={c.name}>{c.name}</option>)}
            </select>
          </div>
          <div style={{ flex: '0 1 160px' }}>
            <label className="ff-label">Sort</label>
            <select className="ff-input" value={sort} onChange={e => { setSort(e.target.value); setPage(1); }}>
              {SORT_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
            </select>
          </div>
          <Button type="submit" size="sm">Search</Button>
          <Button type="button" size="sm" variant="ghost" onClick={() => { setSearch(''); setStatus(''); setCategory(''); setSort('CreatedAt|true'); setPage(1); }}>
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
        <EmptyState title="No requests found" description="Try adjusting your search filters." />
      ) : (
        <Card>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.88rem' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-secondary)' }}>
                <th style={{ padding: '10px', textAlign: 'left' }}>Number</th>
                <th style={{ padding: '10px', textAlign: 'left' }}>Title</th>
                <th style={{ padding: '10px', textAlign: 'left' }}>Requester</th>
                <th style={{ padding: '10px', textAlign: 'left' }}>Category</th>
                <th style={{ padding: '10px', textAlign: 'left' }}>Status</th>
                <th style={{ padding: '10px', textAlign: 'left' }}>Classified</th>
                <th style={{ padding: '10px', textAlign: 'left' }}>Submitted</th>
                <th style={{ padding: '10px' }}></th>
              </tr>
            </thead>
            <tbody>
              {requests.map(r => (
                <tr key={r.id} style={{ borderBottom: '1px solid var(--border-color)' }}>
                  <td style={{ padding: '10px', fontFamily: 'monospace', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>{r.requestNumber}</td>
                  <td style={{ padding: '10px', fontWeight: 600, maxWidth: '240px' }}>{r.title}</td>
                  <td style={{ padding: '10px' }}>{r.requesterName}</td>
                  <td style={{ padding: '10px' }}>{r.category || <span style={{ color: 'var(--text-secondary)' }}>—</span>}</td>
                  <td style={{ padding: '10px' }}><StatusBadge status={r.status} /></td>
                  <td style={{ padding: '10px', textAlign: 'center' }}>
                    {r.hasClassification
                      ? <span style={{ color: 'var(--success-color)', fontWeight: 600 }}>✓</span>
                      : <span style={{ color: 'var(--text-secondary)' }}>—</span>}
                  </td>
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
