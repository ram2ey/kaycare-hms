import apiClient from './client';
import type { FacilitySettingsResponse, SaveFacilitySettingsRequest } from '../types/facilitySettings';

const BASE = '/facility-settings';

export const getFacilitySettings = () =>
  apiClient.get<FacilitySettingsResponse>(BASE).then(r => r.data).catch(err => {
    if (err?.response?.status === 404) return null;
    throw err;
  });

export const saveFacilitySettings = (data: SaveFacilitySettingsRequest) =>
  apiClient.put<FacilitySettingsResponse>(BASE, data).then(r => r.data);

export const uploadLogo = (file: File) => {
  const form = new FormData();
  form.append('file', file);
  return apiClient.post<FacilitySettingsResponse>(`${BASE}/logo`, form, {
    headers: { 'Content-Type': 'multipart/form-data' },
  }).then(r => r.data);
};

export const deleteLogo = () =>
  apiClient.delete<FacilitySettingsResponse>(`${BASE}/logo`).then(r => r.data);
