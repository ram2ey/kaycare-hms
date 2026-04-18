import apiClient from './client';
import type { TenantResponse, CreateTenantRequest, UpdateTenantRequest } from '../types/tenants';

export const getTenants = () =>
  apiClient.get<TenantResponse[]>('/tenants').then(r => r.data);

export const getTenant = (id: string) =>
  apiClient.get<TenantResponse>(`/tenants/${id}`).then(r => r.data);

export const createTenant = (req: CreateTenantRequest) =>
  apiClient.post<TenantResponse>('/tenants', req).then(r => r.data);

export const updateTenant = (id: string, req: UpdateTenantRequest) =>
  apiClient.put<TenantResponse>(`/tenants/${id}`, req).then(r => r.data);

export const activateTenant = (id: string) =>
  apiClient.post<TenantResponse>(`/tenants/${id}/activate`).then(r => r.data);

export const deactivateTenant = (id: string) =>
  apiClient.post<TenantResponse>(`/tenants/${id}/deactivate`).then(r => r.data);

export const deleteTenant = (id: string) =>
  apiClient.delete(`/tenants/${id}`);
