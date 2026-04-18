import apiClient from './client';
import type { BillTemplateResponse, SaveBillTemplateRequest } from '../types/billTemplates';

const BASE = '/bill-templates';

export const getBillTemplates = (params?: { includeInactive?: boolean; category?: string }) =>
  apiClient.get<BillTemplateResponse[]>(BASE, { params }).then(r => r.data);

export const getBillTemplate = (id: string) =>
  apiClient.get<BillTemplateResponse>(`${BASE}/${id}`).then(r => r.data);

export const createBillTemplate = (data: SaveBillTemplateRequest) =>
  apiClient.post<BillTemplateResponse>(BASE, data).then(r => r.data);

export const updateBillTemplate = (id: string, data: SaveBillTemplateRequest) =>
  apiClient.put<BillTemplateResponse>(`${BASE}/${id}`, data).then(r => r.data);

export const deleteBillTemplate = (id: string) =>
  apiClient.delete(`${BASE}/${id}`);

export const toggleBillTemplate = (id: string) =>
  apiClient.put<BillTemplateResponse>(`${BASE}/${id}/toggle-active`).then(r => r.data);
