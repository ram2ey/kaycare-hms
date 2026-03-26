import apiClient from './client';
import type { LabResultResponse, LabResultDetailResponse } from '../types/labResults';

export const getPatientLabResults = (patientId: string) =>
  apiClient.get<LabResultResponse[]>(`/lab-results/patient/${patientId}`).then((r) => r.data);

export const getLabResultById = (id: string) =>
  apiClient.get<LabResultDetailResponse>(`/lab-results/${id}`).then((r) => r.data);

export const getLabResultByAccession = (accessionNumber: string) =>
  apiClient.get<LabResultDetailResponse>(`/lab-results/order/${accessionNumber}`).then((r) => r.data);
