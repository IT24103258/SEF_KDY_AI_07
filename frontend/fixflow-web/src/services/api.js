const BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api';

async function request(endpoint, options = {}) {
  const token = localStorage.getItem('fixflow_token');

  const headers = {
    'Content-Type': 'application/json',
    ...options.headers
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(`${BASE_URL}${endpoint}`, {
    ...options,
    headers
  });

  const contentType = response.headers.get("content-type");
  let data = null;
  
  if (contentType && contentType.includes("application/json")) {
    data = await response.json();
  } else {
    const textData = await response.text();
    if (!response.ok) {
      throw new Error(textData || `HTTP error ${response.status}`);
    }
    data = { message: textData };
  }

  if (!response.ok) {
    let errorMsg = data?.message || data?.title;
    if (data?.errors && typeof data.errors === 'object') {
      const errorList = Object.values(data.errors).flat().filter(Boolean);
      if (errorList.length > 0) {
        errorMsg = errorList.join(' ');
      }
    }
    if (!errorMsg) {
      errorMsg = `HTTP error ${response.status}`;
    }
    throw new Error(errorMsg);
  }

  return data;
}

export const api = {
  get: (endpoint) => request(endpoint, { method: 'GET' }),
  post: (endpoint, body) => request(endpoint, { method: 'POST', body: JSON.stringify(body) }),
  put: (endpoint, body) => request(endpoint, { method: 'PUT', body: JSON.stringify(body) }),
  delete: (endpoint) => request(endpoint, { method: 'DELETE' })
};

// C# Gateway & Standalone Assignment Agent Test URL
const CSHARP_BASE_URL = 'http://localhost:5000/api/technicians';
const STANDALONE_ASSIGNMENT_URL = 'http://localhost:8000/api/agent/assignment/test';

export const technicianApi = {
  fetchRequests: () => api.get('/requests'),
  fetchTechnicians: () => fetch(CSHARP_BASE_URL).then(res => res.json()),

  fetchRecommendation: async (requestData) => {
    const response = await fetch(`${CSHARP_BASE_URL}/assignment-recommendation`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        RequestId: String(requestData.requestId || ""),
        RequiredSkill: String(requestData.requiredSkill || ""),
        PriorityLevel: String(requestData.priorityLevel || "Normal")
      })
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Failed to fetch dynamic recommendation');
    }

    const data = await response.json();
    
    return {
      success: true,
      technicianName: data.technicianName || data.TechnicianName,
      matchScore: data.matchScore || data.MatchScore,
      reasoningSummary: data.reasoningSummary || data.ReasoningSummary,
      recommendedTechnicianId: data.recommendedTechnicianId || data.RecommendedTechnicianId
    };
  },

  assignTechnician: async (assignmentData) => {
    const rawReqId = assignmentData.requestId || assignmentData.request_id || "101";
    const stringReqId = typeof rawReqId === 'number' ? String(rawReqId) : String(rawReqId);
    const numericTechId = String(assignmentData.technicianId || assignmentData.technician_id || "1").replace(/\D/g, '') || "1";

    const payload = {
      RequestId: stringReqId,
      RequiredSkill: String(assignmentData.requiredSkill || assignmentData.required_skill || "Electrical"),
      PriorityLevel: String(assignmentData.priorityLevel || assignmentData.priority || "Medium"),
      TechnicianId: numericTechId
    };

    const response = await fetch(`${CSHARP_BASE_URL}/assign`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    const contentType = response.headers.get("content-type");
    if (contentType && contentType.includes("application/json")) {
      const data = await response.json();
      if (!response.ok) throw new Error(data.message || 'Failed to assign technician');
      return data;
    } else {
      const errorText = await response.text();
      if (!response.ok) throw new Error(errorText || 'Failed to assign technician');
      return { message: errorText };
    }
  },

  fetchMyJobs: async (email) => {
    // If an email is provided, use it as a query parameter for the backend
    const url = email 
      ? `${CSHARP_BASE_URL}/my-jobs?email=${encodeURIComponent(email)}` 
      : `${CSHARP_BASE_URL}/my-jobs`;

    const response = await fetch(url, {
      method: 'GET',
      headers: { 'Content-Type': 'application/json' }
    });
    
    const contentType = response.headers.get("content-type");
    if (contentType && contentType.includes("application/json")) {
      const data = await response.json();
      if (!response.ok) throw new Error(data.message || 'Failed to fetch jobs');
      return data;
    } else {
      const errorText = await response.text();
      if (!response.ok) throw new Error(errorText || 'Failed to fetch jobs');
      return [];
    }
  },
};