import api from './client';
import type {
  ReferralSummaryResponse,
  ReferralDetailResponse,
  CreateReferralRequest,
  UpdateReferralRequest,
  RespondToReferralRequest,
} from '../types/referrals';

export const getReferrals = (status?: string) =>
  api.get<ReferralSummaryResponse[]>('/referrals', { params: status ? { status } : undefined }).then(r => r.data);

export const getIncomingReferrals = () =>
  api.get<ReferralSummaryResponse[]>('/referrals/incoming').then(r => r.data);

export const getReferralsForPatient = (patientId: string) =>
  api.get<ReferralSummaryResponse[]>(`/referrals/patient/${patientId}`).then(r => r.data);

export const getReferral = (id: string) =>
  api.get<ReferralDetailResponse>(`/referrals/${id}`).then(r => r.data);

export const createReferral = (req: CreateReferralRequest) =>
  api.post<ReferralDetailResponse>('/referrals', req).then(r => r.data);

export const updateReferral = (id: string, req: UpdateReferralRequest) =>
  api.put<ReferralDetailResponse>(`/referrals/${id}`, req).then(r => r.data);

export const sendReferral = (id: string) =>
  api.post<ReferralDetailResponse>(`/referrals/${id}/send`).then(r => r.data);

export const respondToReferral = (id: string, req: RespondToReferralRequest) =>
  api.post<ReferralDetailResponse>(`/referrals/${id}/respond`, req).then(r => r.data);

export const cancelReferral = (id: string) =>
  api.post<ReferralDetailResponse>(`/referrals/${id}/cancel`).then(r => r.data);
