import apiClient from './client';
import type { RefundResponse, CreateRefundRequest } from '../types/refunds';

export const getRefunds = (params?: { status?: string; billId?: string; patientId?: string }) =>
  apiClient.get<RefundResponse[]>('/refunds', { params }).then((r) => r.data);

export const getRefund = (id: string) =>
  apiClient.get<RefundResponse>(`/refunds/${id}`).then((r) => r.data);

export const createRefund = (data: CreateRefundRequest) =>
  apiClient.post<RefundResponse>('/refunds', data).then((r) => r.data);

export const processRefund = (id: string) =>
  apiClient.put<RefundResponse>(`/refunds/${id}/process`).then((r) => r.data);

export const cancelRefund = (id: string) =>
  apiClient.put<RefundResponse>(`/refunds/${id}/cancel`).then((r) => r.data);
