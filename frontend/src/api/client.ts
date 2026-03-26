import axios from 'axios';

const apiClient = axios.create({
  baseURL: '/api',
});

// Attach JWT + tenant header on every request
apiClient.interceptors.request.use((config) => {
  const raw = localStorage.getItem('auth');
  if (raw) {
    const auth = JSON.parse(raw);
    config.headers['Authorization'] = `Bearer ${auth.token}`;
    config.headers['X-Tenant-Code'] = auth.tenantCode;
  }
  return config;
});

// Redirect to login on 401
apiClient.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401) {
      localStorage.removeItem('auth');
      window.location.href = '/login';
    }
    return Promise.reject(err);
  }
);

export default apiClient;
