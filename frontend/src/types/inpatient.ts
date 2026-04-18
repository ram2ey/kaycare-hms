export interface WardResponse {
  wardId: string;
  name: string;
  wardType: string;
  description: string | null;
  dailyRate: number;
  isActive: boolean;
  totalBeds: number;
  availableBeds: number;
  occupiedBeds: number;
  maintenanceBeds: number;
  createdAt: string;
  updatedAt: string;
}

export interface SaveWardRequest {
  name: string;
  wardType: string;
  description?: string;
  dailyRate: number;
  isActive: boolean;
}

export interface BedResponse {
  bedId: string;
  wardId: string;
  wardName: string;
  bedNumber: string;
  status: string;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface SaveBedRequest {
  bedNumber: string;
  notes?: string;
}

export interface UpdateBedStatusRequest {
  status: string;
  notes?: string;
}

export const WARD_TYPES = [
  'General', 'ICU', 'HDU', 'Maternity', 'Pediatric',
  'Private', 'Emergency', 'Surgical', 'Psychiatric',
];

export const BED_STATUS_COLORS: Record<string, string> = {
  Available:   'bg-green-100 text-green-700',
  Occupied:    'bg-red-100 text-red-700',
  Maintenance: 'bg-yellow-100 text-yellow-700',
};

export const BED_STATUS_OPTIONS = ['Available', 'Occupied', 'Maintenance'];

// ── Admissions ────────────────────────────────────────────────────────────────

export interface AdmissionTransferResponse {
  admissionTransferId: string;
  fromBedNumber: string;
  fromWardName: string;
  toBedNumber: string;
  toWardName: string;
  transferredAt: string;
  reason: string | null;
  transferredByName: string;
}

export interface AdmissionSummaryResponse {
  admissionId: string;
  admissionNumber: string;
  patientId: string;
  patientName: string;
  patientMrn: string;
  wardName: string;
  bedNumber: string;
  admittingDoctorName: string;
  admissionDate: string;
  expectedDischargeDate: string | null;
  status: string;
  admissionReason: string;
}

export interface AdmissionDetailResponse extends AdmissionSummaryResponse {
  bedId: string;
  wardId: string;
  wardType: string;
  diagnosisOnAdmission: string | null;
  actualDischargeDate: string | null;
  dischargeNotes: string | null;
  dischargeType: string | null;
  createdByName: string;
  createdAt: string;
  updatedAt: string;
  transfers: AdmissionTransferResponse[];
}

export interface AdmitPatientRequest {
  patientId: string;
  bedId: string;
  admittingDoctorUserId: string;
  admissionReason: string;
  diagnosisOnAdmission?: string;
  expectedDischargeDate?: string;
}

export interface DischargePatientRequest {
  dischargeType: string;
  dischargeNotes?: string;
}

export interface TransferPatientRequest {
  newBedId: string;
  reason?: string;
}

// ── Inpatient Billing ─────────────────────────────────────────────────────────

export interface InpatientChargeResponse {
  inpatientChargeId: string;
  admissionId: string;
  chargeDate: string;
  category: string;
  description: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  notes: string | null;
  createdByName: string;
  createdAt: string;
}

export interface SaveInpatientChargeRequest {
  chargeDate: string;
  category: string;
  description: string;
  quantity: number;
  unitPrice: number;
  notes?: string;
}

export interface AccommodationSegment {
  wardName: string;
  wardType: string;
  from: string;
  to: string;
  days: number;
  dailyRate: number;
  total: number;
}

export interface InpatientBillSummaryResponse {
  admissionId: string;
  admissionNumber: string;
  patientName: string;
  patientMrn: string;
  totalCharges: number;
  billId: string | null;
  billNumber: string | null;
  billStatus: string | null;
  accommodationBreakdown: AccommodationSegment[];
  charges: InpatientChargeResponse[];
}

export const INPATIENT_CHARGE_CATEGORIES = [
  'Accommodation', 'Procedure', 'Medication', 'Lab', 'Other',
];

export const CHARGE_CATEGORY_COLORS: Record<string, string> = {
  Accommodation: 'bg-blue-100 text-blue-700',
  Procedure:     'bg-purple-100 text-purple-700',
  Medication:    'bg-green-100 text-green-700',
  Lab:           'bg-yellow-100 text-yellow-700',
  Other:         'bg-gray-100 text-gray-600',
};

export const ADMISSION_STATUS_COLORS: Record<string, string> = {
  Active:     'bg-green-100 text-green-700',
  Discharged: 'bg-gray-100 text-gray-600',
};

// ── Discharge Summary ─────────────────────────────────────────────────────────

export interface DischargeSummaryResponse {
  admissionId: string;
  admissionNumber: string;
  patientId: string;
  patientName: string;
  patientMrn: string;
  patientDateOfBirth: string | null;
  patientGender: string | null;
  patientPhone: string | null;
  wardName: string;
  wardType: string;
  bedNumber: string;
  admittingDoctorName: string;
  admissionDate: string;
  actualDischargeDate: string | null;
  lengthOfStayDays: number | null;
  status: string;
  admissionReason: string;
  diagnosisOnAdmission: string | null;
  dischargeType: string | null;
  dischargeNotes: string | null;
  finalDiagnosis: string | null;
  treatmentSummary: string | null;
  proceduresPerformed: string | null;
  dischargeMedications: string | null;
  dischargeCondition: string | null;
  followUpInstructions: string | null;
  attendingPhysicianNotes: string | null;
  updatedAt: string;
}

export interface UpdateDischargeSummaryRequest {
  finalDiagnosis?: string;
  treatmentSummary?: string;
  proceduresPerformed?: string;
  dischargeMedications?: string;
  dischargeCondition?: string;
  followUpInstructions?: string;
  attendingPhysicianNotes?: string;
}

export const DISCHARGE_CONDITIONS = ['Stable', 'Improved', 'Unchanged', 'Deteriorated', 'Deceased'];

export const DISCHARGE_CONDITIONS_COLORS: Record<string, string> = {
  Stable:       'bg-green-100 text-green-700',
  Improved:     'bg-blue-100 text-blue-700',
  Unchanged:    'bg-gray-100 text-gray-600',
  Deteriorated: 'bg-orange-100 text-orange-700',
  Deceased:     'bg-red-100 text-red-700',
};

export const DISCHARGE_TYPES = ['Regular', 'AMA', 'Transfer', 'Death'];

export const DISCHARGE_TYPE_LABELS: Record<string, string> = {
  Regular:  'Regular Discharge',
  AMA:      'Against Medical Advice',
  Transfer: 'Transfer',
  Death:    'Death',
};
