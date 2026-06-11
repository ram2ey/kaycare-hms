import apiClient from './client';

export interface SoapCopilotResponse {
  subjective: string;
  objective: string;
  assessment: string;
  plan: string;
  primaryCode: string;
  primaryDesc: string;
  secondary: Array<{ code: string; description: string }>;
}

export interface PatientSummaryResponse {
  summary: string;
}

export interface LabInterpreterResponse {
  interpretation: string;
}

export interface DrugSafetyResponse {
  interactions: string;
}

export const getSoapCopilot = (text: string) =>
  apiClient.post<SoapCopilotResponse>('/ai/soap-copilot', { text }).then((r) => r.data);

export const getPatientSummary = (subjective: string, objective: string, assessment: string, plan: string) =>
  apiClient.post<PatientSummaryResponse>('/ai/patient-summary', { subjective, objective, assessment, plan }).then((r) => r.data);

export const getLabInterpreter = (patientName: string, testName: string, results: Array<{ testCode: string; testName: string; value: string; unit: string; refRange: string; flag: string }>) =>
  apiClient.post<LabInterpreterResponse>('/ai/lab-interpreter', { patientName, testName, results }).then((r) => r.data);

export const getDrugSafety = (items: Array<{ drugName: string; genericName: string; dosage: string; quantity: number }>) =>
  apiClient.post<DrugSafetyResponse>('/ai/drug-safety', { items }).then((r) => r.data);
