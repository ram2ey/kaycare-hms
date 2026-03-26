import apiClient from './client';
import type { DocumentResponse } from '../types/documents';

export const getPatientDocuments = (patientId: string) =>
  apiClient.get<DocumentResponse[]>(`/documents/patient/${patientId}`).then((r) => r.data);

export const getDownloadUrl = (id: string) =>
  apiClient.get<{ downloadUrl: string; expiresInMinutes: number }>(`/documents/${id}/download-url`).then((r) => r.data);

export const uploadDocument = (
  patientId: string,
  category: string,
  file: File,
  description?: string,
  consultationId?: string,
) => {
  const form = new FormData();
  form.append('patientId', patientId);
  form.append('category', category);
  form.append('file', file);
  if (description) form.append('description', description);
  if (consultationId) form.append('consultationId', consultationId);
  return apiClient.post<DocumentResponse>('/documents', form, {
    headers: { 'Content-Type': 'multipart/form-data' },
  }).then((r) => r.data);
};

export const deleteDocument = (id: string) =>
  apiClient.delete(`/documents/${id}`);
