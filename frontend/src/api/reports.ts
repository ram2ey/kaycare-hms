import api from './client';
import type {
  ClinicalReportResponse,
  InpatientReportResponse,
  LabReportResponse,
  ReferralReportResponse,
  PharmacyReportResponse,
} from '../types/reports';

type DateRange = { from?: string; to?: string };

export const getClinicalReport  = (p?: DateRange) => api.get<ClinicalReportResponse>('/reports/clinical', { params: p }).then(r => r.data);
export const getInpatientReport = (p?: DateRange) => api.get<InpatientReportResponse>('/reports/inpatient', { params: p }).then(r => r.data);
export const getLabReport       = (p?: DateRange) => api.get<LabReportResponse>('/reports/lab', { params: p }).then(r => r.data);
export const getReferralReport  = (p?: DateRange) => api.get<ReferralReportResponse>('/reports/referrals', { params: p }).then(r => r.data);
export const getPharmacyReport  = (p?: DateRange) => api.get<PharmacyReportResponse>('/reports/pharmacy', { params: p }).then(r => r.data);
