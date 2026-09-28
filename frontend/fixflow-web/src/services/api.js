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
    const errorMsg = data?.message || `HTTP error ${response.status}`;
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
  fetchTechnicians: () => fetch(CSHARP_BASE_URL).then(res => res.json()),

  fetchRecommendation: async (requestData) => {
    try {
      // Extracting a numeric ID from a String ID (e.g. "REQ-101" -> 101)
      const rawReqId = requestData.requestId || requestData.request_id || 101;
      const parsedReqId = typeof rawReqId === 'number' 
        ? rawReqId 
        : parseInt(String(rawReqId).replace(/\D/g, '') || '101', 10);

      // Sending a direct call to the Standalone Endpoint
      const response = await fetch(STANDALONE_ASSIGNMENT_URL, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          request_id: parsedReqId,
          required_skill: requestData.requiredSkill || requestData.category || "Electrical",
          priority: requestData.priority || "High"
        })
      });

      if (response.ok) {
        const data = await response.json();
        const stepResult = data.result;
        const output = stepResult?.output_data;

        if (output) {
          return {
            success: true,
            recommended_candidates: output.recommended_candidates || [],
            top_match_id: output.top_match_id
          };
        }
      }
    } catch (e) {
      console.warn("Standalone Assignment Agent call failed, falling back to C# Gateway...", e);
    }

    // Fallback to C# Gateway
    return fetch(`${CSHARP_BASE_URL}/assignment-recommendation`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(requestData)
    }).then(res => res.json());
  },

  assignTechnician: async (requestId, technicianId) => {
    const numericTechId = String(technicianId).replace(/\D/g, '') || "1";
    const parsedReqId = parseInt(String(requestId).replace(/\D/g, '') || '101', 10);

    const response = await fetch(`${CSHARP_BASE_URL}/assign`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        requestId: parsedReqId,
        technicianId: numericTechId
      })
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

  fetchMyJobs: (email) =>
    fetch(`${CSHARP_BASE_URL}/my-jobs?email=${encodeURIComponent(email)}`).then(res => res.json())
};