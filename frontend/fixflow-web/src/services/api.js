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

  const data = await response.json().catch(() => null);

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
