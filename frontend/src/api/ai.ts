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

export interface ExtractedPrescriptionDrug {
  drugName: string;
  dosage: string;
  frequency: string;
  duration: string;
  quantity: number;
  instructions: string;
}

export interface DischargeSummaryResponse {
  summary: string;
}

export interface InsuranceAppealResponse {
  letter: string;
}

export interface TriageScoreResponse {
  esiScore: number;
  category: string;
  priorityLevel: string;
  rationale: string;
  immediateActions: string[];
  redFlags: string[];
}

export const getSoapCopilot = (text: string) =>
  apiClient.post<SoapCopilotResponse>('/ai/soap-copilot', { text }).then((r) => r.data);

export const getPatientSummary = (subjective: string, objective: string, assessment: string, plan: string) =>
  apiClient.post<PatientSummaryResponse>('/ai/patient-summary', { subjective, objective, assessment, plan }).then((r) => r.data);

export const getLabInterpreter = (patientName: string, testName: string, results: Array<{ testCode: string; testName: string; value: string; unit: string; refRange: string; flag: string }>) =>
  apiClient.post<LabInterpreterResponse>('/ai/lab-interpreter', { patientName, testName, results }).then((r) => r.data);

export const getDrugSafety = (items: Array<{ drugName: string; genericName: string; dosage: string; quantity: number }>) =>
  apiClient.post<DrugSafetyResponse>('/ai/drug-safety', { items }).then((r) => r.data);

export const parsePrescriptionOcr = (base64Image: string, mimeType?: string) =>
  apiClient.post<ExtractedPrescriptionDrug[]>('/ai/prescription-ocr', { base64Image, mimeType }).then((r) => r.data);

export const generateDischargeSummary = (payload: {
  patientName: string;
  ageGender: string;
  mrn: string;
  admissionDate: string;
  dischargeDate: string;
  admissionReason: string;
  consultationNotes: string;
  labResults: string;
  treatmentGiven: string;
  dischargeMedications: string;
}) => apiClient.post<DischargeSummaryResponse>('/ai/discharge-summary', payload).then((r) => r.data);

export const generateInsuranceAppeal = (payload: {
  patientName: string;
  claimNumber: string;
  payerName: string;
  totalAmount: string;
  primaryDiagnosis: string;
  icdCode: string;
  procedureName: string;
  medicalNecessityNotes: string;
  rejectionReason: string;
}) => apiClient.post<InsuranceAppealResponse>('/ai/insurance-appeal', payload).then((r) => r.data);

export const getTriageScore = (payload: {
  patientName: string;
  age: string;
  chiefComplaint: string;
  heartRate: number;
  bloodPressure: string;
  temperature: number;
  spO2: number;
  respiratoryRate: number;
}) => apiClient.post<TriageScoreResponse>('/ai/triage-score', payload).then((r) => r.data);
