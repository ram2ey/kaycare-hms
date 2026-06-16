import apiClient from './client';
import type { PayerTariffResponse, SavePayerTariffRequest } from '../types/payers';

export const getPayerTariffs = (params?: { payerId?: string; serviceCatalogItemId?: string }) =>
  apiClient.get<PayerTariffResponse[]>('/payer-tariffs', { params }).then((r) => r.data);

export const upsertPayerTariff = (data: SavePayerTariffRequest) =>
  apiClient.post<PayerTariffResponse>('/payer-tariffs', data).then((r) => r.data);

export const updatePayerTariff = (id: string, data: SavePayerTariffRequest) =>
  apiClient.put<PayerTariffResponse>(`/payer-tariffs/${id}`, data).then((r) => r.data);

export const deletePayerTariff = (id: string) =>
  apiClient.delete(`/payer-tariffs/${id}`).then((r) => r.data);
