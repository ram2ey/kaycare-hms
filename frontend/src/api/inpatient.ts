import apiClient from './client';
import type {
  WardResponse,
  SaveWardRequest,
  BedResponse,
  SaveBedRequest,
  UpdateBedStatusRequest,
  AdmissionSummaryResponse,
  AdmissionDetailResponse,
  AdmitPatientRequest,
  DischargePatientRequest,
  TransferPatientRequest,
  DischargeSummaryResponse,
  UpdateDischargeSummaryRequest,
  InpatientChargeResponse,
  SaveInpatientChargeRequest,
  InpatientBillSummaryResponse,
} from '../types/inpatient';

export const getWards = (params?: { activeOnly?: boolean }) =>
  apiClient.get<WardResponse[]>('/wards', { params }).then((r) => r.data);

export const getWard = (id: string) =>
  apiClient.get<WardResponse>(`/wards/${id}`).then((r) => r.data);

export const createWard = (data: SaveWardRequest) =>
  apiClient.post<WardResponse>('/wards', data).then((r) => r.data);

export const updateWard = (id: string, data: SaveWardRequest) =>
  apiClient.put<WardResponse>(`/wards/${id}`, data).then((r) => r.data);

export const deactivateWard = (id: string) =>
  apiClient.delete<WardResponse>(`/wards/${id}`).then((r) => r.data);

export const getBeds = (wardId: string) =>
  apiClient.get<BedResponse[]>(`/wards/${wardId}/beds`).then((r) => r.data);

export const addBed = (wardId: string, data: SaveBedRequest) =>
  apiClient.post<BedResponse>(`/wards/${wardId}/beds`, data).then((r) => r.data);

export const updateBed = (bedId: string, data: SaveBedRequest) =>
  apiClient.put<BedResponse>(`/wards/beds/${bedId}`, data).then((r) => r.data);

export const updateBedStatus = (bedId: string, data: UpdateBedStatusRequest) =>
  apiClient.patch<BedResponse>(`/wards/beds/${bedId}/status`, data).then((r) => r.data);

export const deleteBed = (bedId: string) =>
  apiClient.delete(`/wards/beds/${bedId}`);

// ── Admissions ────────────────────────────────────────────────────────────────

export const getAdmissions = (params?: { status?: string; wardId?: string; patientId?: string }) =>
  apiClient.get<AdmissionSummaryResponse[]>('/admissions', { params }).then((r) => r.data);

export const getAdmission = (id: string) =>
  apiClient.get<AdmissionDetailResponse>(`/admissions/${id}`).then((r) => r.data);

export const getPatientAdmissions = (patientId: string) =>
  apiClient.get<AdmissionSummaryResponse[]>(`/admissions/patient/${patientId}`).then((r) => r.data);

export const admitPatient = (data: AdmitPatientRequest) =>
  apiClient.post<AdmissionDetailResponse>('/admissions', data).then((r) => r.data);

export const dischargePatient = (id: string, data: DischargePatientRequest) =>
  apiClient.post<AdmissionDetailResponse>(`/admissions/${id}/discharge`, data).then((r) => r.data);

export const transferPatient = (id: string, data: TransferPatientRequest) =>
  apiClient.post<AdmissionDetailResponse>(`/admissions/${id}/transfer`, data).then((r) => r.data);

// ── Discharge Summary ─────────────────────────────────────────────────────────

export const getDischargeSummary = (admissionId: string) =>
  apiClient.get<DischargeSummaryResponse>(`/admissions/${admissionId}/discharge-summary`).then((r) => r.data);

export const updateDischargeSummary = (admissionId: string, data: UpdateDischargeSummaryRequest) =>
  apiClient.put<DischargeSummaryResponse>(`/admissions/${admissionId}/discharge-summary`, data).then((r) => r.data);

export const downloadDischargeSummaryReport = async (admissionId: string, admissionNumber: string) => {
  const r = await apiClient.get(`/admissions/${admissionId}/discharge-summary/report`, { responseType: 'blob' });
  const url = URL.createObjectURL(new Blob([r.data], { type: 'application/pdf' }));
  const a = document.createElement('a');
  a.href = url; a.download = `DischargeSummary-${admissionNumber}.pdf`;
  a.click(); URL.revokeObjectURL(url);
};

// ── Inpatient Billing ─────────────────────────────────────────────────────────

export const getInpatientCharges = (admissionId: string) =>
  apiClient.get<InpatientChargeResponse[]>(`/admissions/${admissionId}/charges`).then((r) => r.data);

export const getInpatientBillingSummary = (admissionId: string) =>
  apiClient.get<InpatientBillSummaryResponse>(`/admissions/${admissionId}/billing-summary`).then((r) => r.data);

export const addInpatientCharge = (admissionId: string, data: SaveInpatientChargeRequest) =>
  apiClient.post<InpatientChargeResponse>(`/admissions/${admissionId}/charges`, data).then((r) => r.data);

export const updateInpatientCharge = (admissionId: string, chargeId: string, data: SaveInpatientChargeRequest) =>
  apiClient.put<InpatientChargeResponse>(`/admissions/${admissionId}/charges/${chargeId}`, data).then((r) => r.data);

export const removeInpatientCharge = (admissionId: string, chargeId: string) =>
  apiClient.delete(`/admissions/${admissionId}/charges/${chargeId}`);

export const applyAccommodationCharges = (admissionId: string) =>
  apiClient.post<InpatientChargeResponse[]>(`/admissions/${admissionId}/charges/apply-accommodation`).then((r) => r.data);

export const generateInpatientBill = (admissionId: string) =>
  apiClient.post<{ billId: string }>(`/admissions/${admissionId}/generate-bill`).then((r) => r.data);
