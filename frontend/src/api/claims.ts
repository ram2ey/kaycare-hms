import apiClient from './client';
import type {
  InsuranceClaimResponse,
  CreateClaimRequest,
  ApproveClaimRequest,
  RejectClaimRequest,
} from '../types/claims';

export const getClaims = (params?: { status?: string; payerId?: string; patientId?: string }) =>
  apiClient.get<InsuranceClaimResponse[]>('/insurance-claims', { params }).then((r) => r.data);

export const getClaim = (id: string) =>
  apiClient.get<InsuranceClaimResponse>(`/insurance-claims/${id}`).then((r) => r.data);

export const createClaim = (data: CreateClaimRequest) =>
  apiClient.post<InsuranceClaimResponse>('/insurance-claims', data).then((r) => r.data);

export const submitClaim = (id: string) =>
  apiClient.put<InsuranceClaimResponse>(`/insurance-claims/${id}/submit`).then((r) => r.data);

export const approveClaim = (id: string, data: ApproveClaimRequest) =>
  apiClient.put<InsuranceClaimResponse>(`/insurance-claims/${id}/approve`, data).then((r) => r.data);

export const rejectClaim = (id: string, data: RejectClaimRequest) =>
  apiClient.put<InsuranceClaimResponse>(`/insurance-claims/${id}/reject`, data).then((r) => r.data);

export const cancelClaim = (id: string) =>
  apiClient.put<InsuranceClaimResponse>(`/insurance-claims/${id}/cancel`).then((r) => r.data);

export const downloadClaimPdf = (id: string) =>
  apiClient.get(`/insurance-claims/${id}/report`, { responseType: 'blob' }).then((r) => r.data as Blob);
