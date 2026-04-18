import apiClient from './client';
import type {
  PrescriptionTemplateResponse,
  PrescriptionTemplateDetailResponse,
  CreatePrescriptionTemplateRequest,
} from '../types/prescriptionTemplates';

export const getMyTemplates = () =>
  apiClient.get<PrescriptionTemplateResponse[]>('/prescription-templates/mine').then((r) => r.data);

export const getSharedTemplates = () =>
  apiClient.get<PrescriptionTemplateResponse[]>('/prescription-templates/shared').then((r) => r.data);

export const getTemplate = (id: string) =>
  apiClient.get<PrescriptionTemplateDetailResponse>(`/prescription-templates/${id}`).then((r) => r.data);

export const createTemplate = (data: CreatePrescriptionTemplateRequest) =>
  apiClient.post<PrescriptionTemplateDetailResponse>('/prescription-templates', data).then((r) => r.data);

export const updateTemplate = (id: string, data: CreatePrescriptionTemplateRequest) =>
  apiClient.put<PrescriptionTemplateDetailResponse>(`/prescription-templates/${id}`, data).then((r) => r.data);

export const deleteTemplate = (id: string) =>
  apiClient.delete(`/prescription-templates/${id}`);
