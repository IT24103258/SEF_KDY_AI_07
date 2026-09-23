import { api } from './api';

export const priorityApi = {
  // 1. Get priority assessment by request ID
  getPriorityByRequestId: (requestId) => api.get(`/priorities/${requestId}`),

  // 2. Create priority assessment for request ID
  createPriorityAssessment: (requestId, data = {}) => api.post(`/requests/${requestId}/priority-assessments`, data),

  // 3. Update priority assessment (Manager override)
  updatePriorityAssessment: (assessmentId, data) => api.put(`/priority-assessments/${assessmentId}`, data),

  // 4. Get all priority assessments with pagination and filters
  getPriorityAssessments: (params = {}) => {
    const query = new URLSearchParams();
    if (params.searchTerm) query.append('searchTerm', params.searchTerm);
    if (params.priority) query.append('priority', params.priority);
    if (params.riskLevel) query.append('riskLevel', params.riskLevel);
    if (params.assetCriticality) query.append('assetCriticality', params.assetCriticality);
    if (params.escalatedOnly) query.append('escalatedOnly', 'true');
    if (params.page) query.append('page', params.page);
    if (params.pageSize) query.append('pageSize', params.pageSize);

    const queryString = query.toString();
    return api.get(`/priority-assessments${queryString ? `?${queryString}` : ''}`);
  },

  // 5. Search priority assessments
  searchPriorityAssessments: (params = {}) => {
    const query = new URLSearchParams();
    if (params.searchTerm) query.append('searchTerm', params.searchTerm);
    if (params.priority) query.append('priority', params.priority);
    if (params.riskLevel) query.append('riskLevel', params.riskLevel);
    if (params.page) query.append('page', params.page);
    if (params.pageSize) query.append('pageSize', params.pageSize);

    const queryString = query.toString();
    return api.get(`/priority-assessments/search${queryString ? `?${queryString}` : ''}`);
  },

  // 6. Escalate request
  escalateRequest: (requestId, data) => api.post(`/requests/${requestId}/escalate`, data),

  // 7. Get risk assessment details
  getRiskAssessment: (requestId) => api.get(`/requests/${requestId}/risk-assessment`),

  // 8. Simulate risk score and escalation (Sandbox)
  simulateRisk: (requestId, params = {}) => {
    const query = new URLSearchParams();
    if (params.assetCriticality) query.append('assetCriticality', params.assetCriticality);
    if (params.impactLevel) query.append('impactLevel', params.impactLevel);
    if (params.likelihoodLevel) query.append('likelihoodLevel', params.likelihoodLevel);
    if (params.hasSafetyHazard !== undefined) query.append('hasSafetyHazard', params.hasSafetyHazard);
    if (params.recentFailureCount !== undefined) query.append('recentFailureCount', params.recentFailureCount);
    if (params.isHighDensityLocation !== undefined) query.append('isHighDensityLocation', params.isHighDensityLocation);

    const queryString = query.toString();
    return api.get(`/requests/${requestId}/risk-simulation${queryString ? `?${queryString}` : ''}`);
  }
};
