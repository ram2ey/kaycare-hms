import apiClient from './client';
import type { LoginRequest, LoginResponse } from '../types';

export const login = (data: LoginRequest) =>
  apiClient.post<LoginResponse>('/auth/login', data, {
    headers: { 'X-Tenant-Code': data.tenantCode },
  }).then((r) => r.data);

export const changePassword = (data: { currentPassword: string; newPassword: string }) =>
  apiClient.post('/auth/change-password', data).then(r => r.data);
