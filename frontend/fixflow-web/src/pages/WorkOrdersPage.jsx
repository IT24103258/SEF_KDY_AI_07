import React, { useState, useEffect, useCallback } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { PageHeader, Card, Button, StatusBadge, LoadingState, EmptyState } from '../components/SharedUI';
import { WorkOrderModal } from '../components/WorkOrderModal';
import { workOrderApi } from '../services/workOrderApi';
import {
  Search,
  Filter,
  Plus,
  Calendar,
  Clock,
  AlertTriangle,
  ChevronLeft,
  ChevronRight,
  Eye,
  Pencil,
  Trash2,
  CheckCircle2,
  AlertCircle
} from 'lucide-react';

export const WorkOrdersPage = () => {
  const navigate = useNavigate();

  const [workOrders, setWorkOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Pagination & Filtering state
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [technicianId, setTechnicianId] = useState('');
  const [dateFilter, setDateFilter] = useState('');
  const [sortBy, setSortBy] = useState('newest');
  const [technicians, setTechnicians] = useState([]);

  // Modal State
  const [modalOpen, setModalOpen] = useState(false);
  const [selectedWorkOrder, setSelectedWorkOrder] = useState(null);

  const fetchWorkOrders = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await workOrderApi.getWorkOrders({
        page,
        pageSize,
        search,
        status: status || undefined,
        technicianId: technicianId || undefined,
        startDate: dateFilter || undefined,
        sortBy: sortBy === 'scheduled' ? 'scheduledStartTime' : 'createdAt',
        sortDirection: sortBy === 'oldest' ? 'asc' : 'desc'
      });

      if (res?.success && res.data) {
        setWorkOrders(res.data.items || []);
        setTotalCount(res.data.totalCount || 0);
      }
    } catch (err) {
      setError(err.message || 'Failed to fetch work orders.');
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, search, status, technicianId, dateFilter, sortBy]);

  useEffect(() => {
    fetchWorkOrders();
  }, [fetchWorkOrders]);

  useEffect(() => {
    workOrderApi.getUsers().then((res) => {
      if (res?.success) setTechnicians(res.data || []);
    }).catch(() => {});
  }, []);

  const handleDelete = async (id, woNumber) => {
    if (window.confirm(`Are you sure you want to cancel / delete Work Order ${woNumber}?`)) {
      try {
        await workOrderApi.deleteWorkOrder(id);
        fetchWorkOrders();
      } catch (err) {
        alert(err.message || 'Failed to delete work order.');
      }
    }
  };

  const totalPages = Math.ceil(totalCount / pageSize) || 1;

  const PriorityBadge = ({ priorityLevel }) => {
    const getStyle = () => {
      switch (priorityLevel?.toLowerCase()) {
        case 'critical':
          return { bg: 'rgba(255, 107, 113, 0.16)', color: 'var(--danger-color)', border: '1px solid var(--danger-color)' };
        case 'high':
          return { bg: 'rgba(251, 191, 36, 0.16)', color: 'var(--warning-color)', border: '1px solid var(--warning-color)' };
        case 'medium':
          return { bg: 'rgba(59, 130, 246, 0.16)', color: '#3b82f6', border: '1px solid #3b82f6' };
        default:
          return { bg: 'var(--glass-bg)', color: 'var(--text-secondary)', border: '1px solid var(--border-color)' };
      }
    };
    const s = getStyle();
    return (
      <span
        style={{
          padding: '2px 8px',
          borderRadius: '12px',
          fontSize: '0.75rem',
          fontWeight: '600',
          backgroundColor: s.bg,
          color: s.color,
          border: s.border,
          display: 'inline-flex',
          alignItems: 'center',
          gap: '4px'
        }}
      >
        {priorityLevel}
      </span>
    );
  };

  return (
    <div>
      <PageHeader
        title="Work Orders"
        description="Comprehensive lifecycle dispatching, AI schedule proposals &amp; field execution"
        action={
          <Button
            variant="primary"
            onClick={() => {
              setSelectedWorkOrder(null);
              setModalOpen(true);
            }}
            style={{ display: 'flex', alignItems: 'center', gap: '6px' }}
          >
            <Plus size={16} /> New Work Order
          </Button>
        }
      />

      {/* Filter & Search Toolbar */}
      <Card style={{ marginBottom: '1.5rem', padding: '1rem' }}>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(170px, 1fr))',
            gap: '0.75rem',
            alignItems: 'center'
          }}
        >
          {/* Search */}
          <div className="ff-input-wrap">
            <Search size={16} className="ff-input-icon" />
            <input
              type="text"
              className="ff-input ff-input-has-icon"
              placeholder="Search work orders..."
              value={search}
              onChange={(e) => {
                setSearch(e.target.value);
                setPage(1);
              }}
            />
          </div>

          {/* Status Filter */}
          <select
            className="ff-input"
            value={status}
            onChange={(e) => {
              setStatus(e.target.value);
              setPage(1);
            }}
          >
            <option value="">All Statuses</option>
            <option value="Draft">Draft</option>
            <option value="PendingManagerApproval">Pending</option>
            <option value="Approved">Approved</option>
            <option value="Scheduled">Scheduled</option>
            <option value="InProgress">In Progress</option>
            <option value="Paused">Paused</option>
            <option value="Completed">Completed</option>
            <option value="Cancelled">Cancelled</option>
            <option value="Rejected">Rejected</option>
            <option value="RevisionRequested">Revision Requested</option>
          </select>

          {/* Technician Filter */}
          <select
            className="ff-input"
            value={technicianId}
            onChange={(e) => {
              setTechnicianId(e.target.value);
              setPage(1);
            }}
          >
            <option value="">All Technicians</option>
            {technicians.map((t) => (
              <option key={t.id} value={t.id}>
                {t.firstName} {t.lastName}
              </option>
            ))}
          </select>

          {/* Date Filter */}
          <input
            type="date"
            className="ff-input"
            title="Filter by Scheduled Date"
            value={dateFilter}
            onChange={(e) => {
              setDateFilter(e.target.value);
              setPage(1);
            }}
          />

          {/* Sort Control */}
          <select
            className="ff-input"
            value={sortBy}
            onChange={(e) => {
              setSortBy(e.target.value);
              setPage(1);
            }}
          >
            <option value="newest">Sort: Newest First</option>
            <option value="oldest">Sort: Oldest First</option>
            <option value="scheduled">Sort: Scheduled Date</option>
          </select>
        </div>
      </Card>

      {/* Main Table View */}
      {loading ? (
        <LoadingState message="Loading work orders from PostgreSQL database..." />
      ) : error ? (
        <Card>
          <div style={{ color: 'var(--danger-color)', padding: '1rem', textAlign: 'center' }}>
            <AlertCircle size={24} style={{ marginBottom: '8px' }} />
            <p>{error}</p>
          </div>
        </Card>
      ) : workOrders.length === 0 ? (
        <EmptyState
          title="No Work Orders Found"
          description="No work orders match the selected filters or search query."
        />
      ) : (
        <Card style={{ padding: 0, overflow: 'hidden' }}>
          <div style={{ overflowX: 'auto' }}>
            <table
              style={{
                width: '100%',
                borderCollapse: 'collapse',
                textAlign: 'left',
                fontSize: '0.88rem'
              }}
            >
              <thead>
                <tr
                  style={{
                    borderBottom: '1px solid var(--border-color)',
                    backgroundColor: 'var(--glass-bg)',
                    color: 'var(--text-secondary)',
                    fontWeight: 600
                  }}
                >
                  <th style={{ padding: '12px 16px' }}>ID</th>
                  <th style={{ padding: '12px 16px' }}>Request</th>
                  <th style={{ padding: '12px 16px' }}>Technician</th>
                  <th style={{ padding: '12px 16px' }}>Scheduled</th>
                  <th style={{ padding: '12px 16px' }}>Status</th>
                  <th style={{ padding: '12px 16px', textAlign: 'right' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {workOrders.map((wo) => {
                  const isConflict = wo.conflictDetected;
                  return (
                    <tr
                      key={wo.id}
                      style={{
                        borderBottom: '1px solid var(--border-color)',
                        backgroundColor: isConflict ? 'rgba(255, 107, 113, 0.04)' : 'transparent',
                        transition: 'background-color 0.15s'
                      }}
                    >
                      <td style={{ padding: '12px 16px', fontWeight: 650 }}>
                        <Link
                          to={`/work-orders/${wo.id}`}
                          style={{
                            color: 'var(--primary-color)',
                            textDecoration: 'none'
                          }}
                        >
                          {wo.workOrderNumber}
                        </Link>
                        {isConflict && (
                          <span
                            title="Schedule Conflict Detected"
                            style={{
                              marginLeft: '6px',
                              color: 'var(--danger-color)',
                              display: 'inline-flex',
                              alignItems: 'center'
                            }}
                          >
                            <AlertTriangle size={14} />
                          </span>
                        )}
                      </td>

                      <td style={{ padding: '12px 16px' }}>
                        <div style={{ fontWeight: 600, color: 'var(--text-primary)' }}>{wo.title}</div>
                        <div style={{ fontSize: '0.78rem', color: 'var(--text-secondary)' }}>
                          {wo.requestNumber ? `${wo.requestNumber} • ` : ''}{wo.locationName}
                        </div>
                      </td>

                      <td style={{ padding: '12px 16px' }}>
                        <span style={{ fontWeight: 500 }}>{wo.technicianName}</span>
                      </td>

                      <td style={{ padding: '12px 16px', color: 'var(--text-secondary)', fontSize: '0.82rem' }}>
                        {wo.scheduledStartTime ? (
                          <div style={{ display: 'flex', flexDirection: 'column' }}>
                            <span style={{ fontWeight: 600, color: 'var(--text-primary)' }}>
                              {new Date(wo.scheduledStartTime).toLocaleDateString()}
                            </span>
                            <span>
                              {new Date(wo.scheduledStartTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} -{' '}
                              {wo.scheduledEndTime ? new Date(wo.scheduledEndTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : `${wo.estimatedDurationMinutes}m`}
                            </span>
                          </div>
                        ) : (
                          <span style={{ fontStyle: 'italic' }}>Unscheduled Draft</span>
                        )}
                      </td>

                      <td style={{ padding: '12px 16px' }}>
                        <StatusBadge status={wo.status} />
                      </td>

                      <td style={{ padding: '12px 16px', textAlign: 'right' }}>
                        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '6px' }}>
                          <Button
                            variant="secondary"
                            size="sm"
                            title="View Details"
                            onClick={() => navigate(`/work-orders/${wo.id}`)}
                            style={{ padding: '4px 8px' }}
                          >
                            <Eye size={14} />
                          </Button>
                          <Button
                            variant="secondary"
                            size="sm"
                            title="Edit"
                            onClick={() => {
                              setSelectedWorkOrder(wo);
                              setModalOpen(true);
                            }}
                            style={{ padding: '4px 8px' }}
                          >
                            <Pencil size={14} />
                          </Button>
                          <Button
                            variant="danger"
                            size="sm"
                            title="Delete"
                            onClick={() => handleDelete(wo.id, wo.workOrderNumber)}
                            style={{ padding: '4px 8px' }}
                          >
                            <Trash2 size={14} />
                          </Button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {/* Server-Side Pagination Controls */}
          <div
            style={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              padding: '1rem 1.5rem',
              borderTop: '1px solid var(--border-color)',
              backgroundColor: 'var(--glass-bg)'
            }}
          >
            <span style={{ fontSize: '0.82rem', color: 'var(--text-secondary)' }}>
              Showing {totalCount > 0 ? (page - 1) * pageSize + 1 : 0} to{' '}
              {Math.min(page * pageSize, totalCount)} of {totalCount} work orders
            </span>

            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <Button
                variant="secondary"
                size="sm"
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
              >
                <ChevronLeft size={14} /> Previous
              </Button>

              <span style={{ fontSize: '0.82rem', fontWeight: 600, padding: '0 8px' }}>
                Page {page} of {totalPages}
              </span>

              <Button
                variant="secondary"
                size="sm"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              >
                Next <ChevronRight size={14} />
              </Button>
            </div>
          </div>
        </Card>
      )}

      {/* Create / Edit Modal */}
      <WorkOrderModal
        isOpen={modalOpen}
        workOrder={selectedWorkOrder}
        onClose={() => setModalOpen(false)}
        onSaved={fetchWorkOrders}
      />
    </div>
  );
};
