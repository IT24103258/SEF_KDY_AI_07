import { api } from './api';

export const workOrderApi = {
  // Work Orders List & Search
  getWorkOrders: async (params = {}) => {
    const query = new URLSearchParams();
    if (params.page) query.append('page', params.page);
    if (params.pageSize) query.append('pageSize', params.pageSize);
    if (params.search) query.append('search', params.search);
    if (params.status) query.append('status', params.status);
    if (params.technicianId) query.append('technicianId', params.technicianId);
    if (params.priority) query.append('priority', params.priority);
    if (params.startDate) query.append('startDate', params.startDate);
    if (params.endDate) query.append('endDate', params.endDate);
    if (params.sortBy) query.append('sortBy', params.sortBy);
    if (params.sortDirection) query.append('sortDirection', params.sortDirection);

    const queryString = query.toString();
    const endpoint = `/work-orders${queryString ? `?${queryString}` : ''}`;
    return await api.get(endpoint);
  },

  // Work Order Details
  getWorkOrderById: async (id) => {
    return await api.get(`/work-orders/${id}`);
  },

  // Create Work Order
  createWorkOrder: async (data) => {
    return await api.post('/work-orders', data);
  },

  // Update Work Order
  updateWorkOrder: async (id, data) => {
    return await api.put(`/work-orders/${id}`, data);
  },

  // Delete / Cancel Work Order
  deleteWorkOrder: async (id) => {
    return await api.delete(`/work-orders/${id}`);
  },

  // Update Status
  updateStatus: async (id, status, reason = '') => {
    return await api.post(`/work-orders/${id}/status`, { status, reason });
  },

  // Human Approval Actions
  approveWorkOrder: async (id, comments = '') => {
    return await api.post(`/work-orders/${id}/approve`, { approved: true, comments });
  },

  rejectWorkOrder: async (id, comments = '') => {
    return await api.post(`/work-orders/${id}/reject`, { approved: false, comments });
  },

  requestRevision: async (id, revisionNotes = '') => {
    return await api.post(`/work-orders/${id}/request-revision`, { requestRevision: true, revisionNotes });
  },

  // Pending Approvals Queue
  getPendingApprovals: async () => {
    return await api.get('/work-orders/pending-approval');
  },

  // Scheduling Proposal Trigger
  scheduleRequest: async (requestId, data) => {
    return await api.post(`/requests/${requestId}/schedule`, data);
  },

  // Deterministic Validation
  validateSchedule: async (data) => {
    return await api.post('/scheduling/validate', data);
  },

  // Calendar
  getCalendarEvents: async (params = {}) => {
    const query = new URLSearchParams();
    if (params.startDate) query.append('startDate', params.startDate);
    if (params.endDate) query.append('endDate', params.endDate);
    if (params.technicianId) query.append('technicianId', params.technicianId);
    if (params.priority) query.append('priority', params.priority);
    if (params.status) query.append('status', params.status);

    const queryString = query.toString();
    const endpoint = `/calendar${queryString ? `?${queryString}` : ''}`;
    return await api.get(endpoint);
  },

  // Operational Reports
  getReports: async (params = {}) => {
    const query = new URLSearchParams();
    if (params.startDate) query.append('startDate', params.startDate);
    if (params.endDate) query.append('endDate', params.endDate);
    const queryString = query.toString();
    return await api.get(`/reports/work-orders${queryString ? `?${queryString}` : ''}`);
  },

  // Notes
  addNote: async (workOrderId, noteText) => {
    return await api.post(`/work-orders/${workOrderId}/notes`, { noteText });
  },

  // Completion Sign-off
  completeWorkOrder: async (workOrderId, data) => {
    return await api.post(`/work-orders/${workOrderId}/complete`, data);
  },

  // Supporting lookups
  getLocations: async () => {
    return await api.get('/locations');
  },

  getUsers: async () => {
    return await api.get('/users');
  }
};
