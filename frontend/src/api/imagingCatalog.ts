import apiClient from './client';
import type { ImagingProcedureItem, SaveImagingProcedureRequest } from '../types/radiology';

export const getImagingCatalogAll = () =>
  apiClient.get<ImagingProcedureItem[]>('/radiology-orders/catalog/all').then((r) => r.data);

export const createImagingProcedure = (data: SaveImagingProcedureRequest) =>
  apiClient.post<ImagingProcedureItem>('/radiology-orders/catalog', data).then((r) => r.data);

export const updateImagingProcedure = (id: string, data: SaveImagingProcedureRequest) =>
  apiClient.put<ImagingProcedureItem>(`/radiology-orders/catalog/${id}`, data).then((r) => r.data);

export const toggleImagingProcedure = (id: string) =>
  apiClient.patch<ImagingProcedureItem>(`/radiology-orders/catalog/${id}/toggle`).then((r) => r.data);
