import apiClient from './client';
import type {
  PrescriptionResponse,
  PrescriptionDetailResponse,
  CreatePrescriptionRequest,
} from '../types/prescriptions';

export const getPending = () =>
  apiClient.get<PrescriptionResponse[]>('/prescriptions/pending').then((r) => r.data);

export const getPatientPrescriptions = (patientId: string) =>
  apiClient.get<PrescriptionResponse[]>(`/prescriptions/patient/${patientId}`).then((r) => r.data);

export const getByConsultation = (consultationId: string) =>
  apiClient.get<PrescriptionDetailResponse[]>(`/prescriptions/consultation/${consultationId}`).then((r) => r.data);

export const getPrescription = (id: string) =>
  apiClient.get<PrescriptionDetailResponse>(`/prescriptions/${id}`).then((r) => r.data);

export const createPrescription = (data: CreatePrescriptionRequest) =>
  apiClient.post<PrescriptionDetailResponse>('/prescriptions', data).then((r) => r.data);

export const dispensePrescription = (id: string, notes?: string) =>
  apiClient.post<PrescriptionDetailResponse>(`/prescriptions/${id}/dispense`, { notes }).then((r) => r.data);

export const cancelPrescription = (id: string) =>
  apiClient.post<PrescriptionDetailResponse>(`/prescriptions/${id}/cancel`).then((r) => r.data);
