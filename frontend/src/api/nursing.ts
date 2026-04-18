import apiClient from './client';
import type {
  VitalSignsResponse,
  RecordVitalSignsRequest,
  NursingNoteResponse,
  AddNursingNoteRequest,
  MARItemResponse,
  MAREntryResponse,
  RecordAdministrationRequest,
} from '../types/nursing';

// Vital Signs
export const getVitalsForPatient = (patientId: string, limit = 20) =>
  apiClient.get<VitalSignsResponse[]>(`/patients/${patientId}/vitals?limit=${limit}`).then(r => r.data);

export const getLatestVitals = (patientId: string) =>
  apiClient.get<VitalSignsResponse>(`/patients/${patientId}/vitals/latest`).then(r => r.data);

export const getVitalsForAdmission = (admissionId: string) =>
  apiClient.get<VitalSignsResponse[]>(`/admissions/${admissionId}/vitals`).then(r => r.data);

export const recordVitals = (patientId: string, data: RecordVitalSignsRequest) =>
  apiClient.post<VitalSignsResponse>(`/patients/${patientId}/vitals`, data).then(r => r.data);

// Nursing Notes
export const getNursingNotesForPatient = (patientId: string) =>
  apiClient.get<NursingNoteResponse[]>(`/patients/${patientId}/nursing-notes`).then(r => r.data);

export const getNursingNotesForAdmission = (admissionId: string) =>
  apiClient.get<NursingNoteResponse[]>(`/admissions/${admissionId}/nursing-notes`).then(r => r.data);

export const addNursingNote = (patientId: string, data: AddNursingNoteRequest) =>
  apiClient.post<NursingNoteResponse>(`/patients/${patientId}/nursing-notes`, data).then(r => r.data);

export const deleteNursingNote = (noteId: string) =>
  apiClient.delete(`/nursing-notes/${noteId}`);

// MAR
export const getMARForPatient = (patientId: string) =>
  apiClient.get<MARItemResponse[]>(`/mar/patient/${patientId}`).then(r => r.data);

export const getMARForAdmission = (admissionId: string) =>
  apiClient.get<MARItemResponse[]>(`/mar/admission/${admissionId}`).then(r => r.data);

export const recordAdministration = (data: RecordAdministrationRequest) =>
  apiClient.post<MAREntryResponse>('/mar', data).then(r => r.data);
