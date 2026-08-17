import apiClient from './client';
import { setCsrfToken } from './csrfToken';
import type { LoginRequest, LoginResponse, MeResponse } from '../types';

export const login = (data: LoginRequest) =>
  apiClient.post<LoginResponse>('/auth/login', data, {
    headers: { 'X-Tenant-Code': data.tenantCode },
  }).then((r) => {
    setCsrfToken(r.data.csrfToken);
    return r.data;
  });

export const getMe = () =>
  apiClient.get<MeResponse>('/auth/me').then((r) => {
    setCsrfToken(r.data.csrfToken);
    return r.data;
  });

export const logout = () =>
  apiClient.post('/auth/logout').finally(() => setCsrfToken(null));

export const changePassword = (data: { currentPassword: string; newPassword: string }) =>
  apiClient.post('/auth/change-password', data).then(r => r.data);
