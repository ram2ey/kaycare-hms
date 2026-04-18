export interface ReferralSummaryResponse {
  referralId: string;
  referralNumber: string;
  patientName: string;
  patientMrn: string;
  referringDoctorName: string;
  referredToDoctorName: string | null;
  referredToDepartment: string | null;
  referralType: string;
  externalFacility: string | null;
  urgency: string;
  status: string;
  createdAt: string;
}

export interface ReferralDetailResponse {
  referralId: string;
  referralNumber: string;
  patientId: string;
  patientName: string;
  patientMrn: string;
  referringDoctorUserId: string;
  referringDoctorName: string;
  referredToDoctorUserId: string | null;
  referredToDoctorName: string | null;
  referredToDepartment: string | null;
  referralType: string;
  externalFacility: string | null;
  urgency: string;
  reason: string;
  clinicalNotes: string | null;
  status: string;
  responseNotes: string | null;
  consultationId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateReferralRequest {
  patientId: string;
  consultationId?: string;
  referredToDoctorUserId?: string;
  referredToDepartment?: string;
  referralType: string;
  externalFacility?: string;
  urgency: string;
  reason: string;
  clinicalNotes?: string;
}

export interface UpdateReferralRequest {
  referredToDoctorUserId?: string;
  referredToDepartment?: string;
  referralType: string;
  externalFacility?: string;
  urgency: string;
  reason: string;
  clinicalNotes?: string;
}

export interface RespondToReferralRequest {
  action: string;
  responseNotes?: string;
}

export const REFERRAL_URGENCY_COLORS: Record<string, string> = {
  Routine:   'bg-blue-100 text-blue-700',
  Urgent:    'bg-orange-100 text-orange-700',
  Emergency: 'bg-red-100 text-red-700',
};

export const REFERRAL_STATUS_COLORS: Record<string, string> = {
  Draft:     'bg-gray-100 text-gray-600',
  Sent:      'bg-blue-100 text-blue-700',
  Accepted:  'bg-teal-100 text-teal-700',
  Completed: 'bg-green-100 text-green-700',
  Declined:  'bg-red-100 text-red-700',
  Cancelled: 'bg-gray-200 text-gray-500',
};
