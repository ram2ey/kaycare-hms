import apiClient from './client';
import type { PagedResult } from '../types';
import type {
  PatientResponse,
  PatientDetailResponse,
  AllergyResponse,
  CreatePatientRequest,
  AddAllergyRequest,
  PatientSearchRequest,
} from '../types/patients';

export const searchPatients = (params: PatientSearchRequest) =>
  apiClient.get<PagedResult<PatientResponse>>('/patients', { params }).then((r) => r.data);

export const getPatient = (id: string) =>
  apiClient.get<PatientDetailResponse>(`/patients/${id}`).then((r) => r.data);

export const createPatient = (data: CreatePatientRequest) =>
  apiClient.post<PatientDetailResponse>('/patients', data).then((r) => r.data);

export const getAllergies = (patientId: string) =>
  apiClient.get<AllergyResponse[]>(`/patients/${patientId}/allergies`).then((r) => r.data);

export const addAllergy = (patientId: string, data: AddAllergyRequest) =>
  apiClient.post<AllergyResponse>(`/patients/${patientId}/allergies`, data).then((r) => r.data);

export const removeAllergy = (patientId: string, allergyId: string) =>
  apiClient.delete(`/patients/${patientId}/allergies/${allergyId}`);
