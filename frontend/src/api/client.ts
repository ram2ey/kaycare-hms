import axios from 'axios';
import { getCsrfToken } from './csrfToken';

// Opt-out for the global 401 redirect below. Only set this on silent background polls that
// already render their own failure state (e.g. CriticalAlertsWidget) - a transient failure on
// one of those must never be able to force-navigate the whole app away from foreground work
// just because that one call happened to 401.
declare module 'axios' {
  export interface AxiosRequestConfig {
    skipAuthRedirect?: boolean;
  }
}

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '/api',
  withCredentials: true,
});

const UNSAFE_METHODS = new Set(['post', 'put', 'patch', 'delete']);

// Attach the CSRF header on mutating requests. The JWT itself travels via the httpOnly auth
// cookie automatically (withCredentials above) — it's never read or attached from JS.
apiClient.interceptors.request.use((config) => {
  const method = (config.method ?? 'get').toLowerCase();
  if (UNSAFE_METHODS.has(method)) {
    const token = getCsrfToken();
    if (token) config.headers['X-XSRF-TOKEN'] = token;
  }
  return config;
});

// Redirect to login on 401
apiClient.interceptors.response.use(
  (res) => res,
  (err) => {
    const skipRedirect = err.config?.skipAuthRedirect === true;
    if (!skipRedirect && err.response?.status === 401 && !window.location.pathname.includes('/login')) {
      window.location.href = '/login';
    }
    return Promise.reject(err);
  }
);

export default apiClient;
