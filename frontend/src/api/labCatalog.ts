import apiClient from './client';
import type { LabTestCatalog, SaveLabTestRequest } from '../types/labOrders';

export const getLabCatalogAll = () =>
  apiClient.get<LabTestCatalog[]>('/lab-orders/catalog/all').then((r) => r.data);

export const createLabTest = (data: SaveLabTestRequest) =>
  apiClient.post<LabTestCatalog>('/lab-orders/catalog', data).then((r) => r.data);

export const updateLabTest = (id: string, data: SaveLabTestRequest) =>
  apiClient.put<LabTestCatalog>(`/lab-orders/catalog/${id}`, data).then((r) => r.data);

export const toggleLabTest = (id: string) =>
  apiClient.patch<LabTestCatalog>(`/lab-orders/catalog/${id}/toggle`).then((r) => r.data);
