import apiClient from './client';
import type { PayerResponse, SavePayerRequest } from '../types/payers';

export const getPayers = (activeOnly = true) =>
  apiClient.get<PayerResponse[]>('/payers', { params: { activeOnly } }).then((r) => r.data);

export const getPayer = (id: string) =>
  apiClient.get<PayerResponse>(`/payers/${id}`).then((r) => r.data);

export const createPayer = (data: SavePayerRequest) =>
  apiClient.post<PayerResponse>('/payers', data).then((r) => r.data);

export const updatePayer = (id: string, data: SavePayerRequest) =>
  apiClient.put<PayerResponse>(`/payers/${id}`, data).then((r) => r.data);

export const deletePayer = (id: string) =>
  apiClient.delete(`/payers/${id}`);
