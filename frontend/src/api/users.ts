import apiClient from './client';
import type { UserResponse, CreateUserRequest, UpdateUserRequest, ResetPasswordRequest, DepartmentSummary } from '../types/users';

const BASE = '/users';

export const getUsers = (params?: { role?: string; includeInactive?: boolean }) =>
  apiClient.get<UserResponse[]>(BASE, { params }).then(r => r.data);

export const getUser = (id: string) =>
  apiClient.get<UserResponse>(`${BASE}/${id}`).then(r => r.data);

export const createUser = (data: CreateUserRequest) =>
  apiClient.post<UserResponse>(BASE, data).then(r => r.data);

export const updateUser = (id: string, data: UpdateUserRequest) =>
  apiClient.put<UserResponse>(`${BASE}/${id}`, data).then(r => r.data);

export const deactivateUser = (id: string) =>
  apiClient.put(`${BASE}/${id}/deactivate`).then(r => r.data);

export const reactivateUser = (id: string) =>
  apiClient.put(`${BASE}/${id}/reactivate`).then(r => r.data);

export const resetPassword = (id: string, data: ResetPasswordRequest) =>
  apiClient.put(`${BASE}/${id}/reset-password`, data).then(r => r.data);

export const getDepartments = () =>
  apiClient.get<DepartmentSummary[]>(`${BASE}/departments`).then(r => r.data);

export const renameDepartment = (oldName: string, newName: string) =>
  apiClient.put(`${BASE}/departments/rename`, { oldName, newName });
