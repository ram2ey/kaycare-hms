import apiClient from './client';
import type { ServiceCatalogItem, SaveServiceCatalogItemRequest } from '../types/serviceCatalog';

export const getCatalog = (activeOnly = true) =>
  apiClient.get<ServiceCatalogItem[]>('/service-catalog', { params: { activeOnly } }).then((r) => r.data);

export const createCatalogItem = (data: SaveServiceCatalogItemRequest) =>
  apiClient.post<ServiceCatalogItem>('/service-catalog', data).then((r) => r.data);

export const updateCatalogItem = (id: string, data: SaveServiceCatalogItemRequest) =>
  apiClient.put<ServiceCatalogItem>(`/service-catalog/${id}`, data).then((r) => r.data);

export const deleteCatalogItem = (id: string) =>
  apiClient.delete(`/service-catalog/${id}`);
