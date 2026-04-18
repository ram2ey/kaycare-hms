import apiClient from './client';
import type {
  ConsultationSummaryResponse,
  ConsultationDetailResponse,
  CreateConsultationRequest,
  UpdateConsultationRequest,
} from '../types/consultations';

export const getPatientConsultations = (patientId: string) =>
  apiClient.get<ConsultationSummaryResponse[]>(`/consultations/patient/${patientId}`).then((r) => r.data);

export const getByAppointment = (appointmentId: string) =>
  apiClient.get<ConsultationDetailResponse>(`/consultations/appointment/${appointmentId}`).then((r) => r.data);

export const getConsultation = (id: string) =>
  apiClient.get<ConsultationDetailResponse>(`/consultations/${id}`).then((r) => r.data);

export const createConsultation = (data: CreateConsultationRequest) =>
  apiClient.post<ConsultationDetailResponse>('/consultations', data).then((r) => r.data);

export const updateConsultation = (id: string, data: UpdateConsultationRequest) =>
  apiClient.put<ConsultationDetailResponse>(`/consultations/${id}`, data).then((r) => r.data);

export const signConsultation = (id: string) =>
  apiClient.post<ConsultationDetailResponse>(`/consultations/${id}/sign`).then((r) => r.data);
