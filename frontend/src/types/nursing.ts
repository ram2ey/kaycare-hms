export interface VitalSignsResponse {
  vitalSignsId: string;
  patientId: string;
  admissionId: string | null;
  consultationId: string | null;
  recordedByName: string;
  recordedAt: string;
  bloodPressureSystolic: number | null;
  bloodPressureDiastolic: number | null;
  pulseRate: number | null;
  temperature: number | null;
  spO2: number | null;
  respiratoryRate: number | null;
  weight: number | null;
  height: number | null;
  bmi: number | null;
  notes: string | null;
}

export interface RecordVitalSignsRequest {
  admissionId?: string | null;
  consultationId?: string | null;
  bloodPressureSystolic?: number | null;
  bloodPressureDiastolic?: number | null;
  pulseRate?: number | null;
  temperature?: number | null;
  spO2?: number | null;
  respiratoryRate?: number | null;
  weight?: number | null;
  height?: number | null;
  notes?: string | null;
}

export interface NursingNoteResponse {
  nursingNoteId: string;
  patientId: string;
  admissionId: string | null;
  authorName: string;
  noteType: string;
  note: string;
  createdAt: string;
}

export interface AddNursingNoteRequest {
  admissionId?: string | null;
  noteType: string;
  note: string;
}

export interface MAREntryResponse {
  medicationAdministrationId: string;
  prescriptionId: string;
  prescriptionItemId: string;
  medicationName: string;
  dosage: string;
  frequency: string;
  administeredByName: string;
  administeredAt: string;
  status: string;
  doseGiven: string | null;
  route: string | null;
  notes: string | null;
}

export interface MARItemResponse {
  prescriptionItemId: string;
  prescriptionId: string;
  patientId: string;
  medicationName: string;
  dosage: string;
  frequency: string;
  instructions: string;
  administrations: MAREntryResponse[];
}

export interface RecordAdministrationRequest {
  prescriptionId: string;
  prescriptionItemId: string;
  patientId: string;
  admissionId?: string | null;
  status: string;
  doseGiven?: string | null;
  route?: string | null;
  notes?: string | null;
}

export const NOTE_TYPES = ['General', 'Assessment', 'Handover', 'Procedure'] as const;

export const MAR_STATUSES = ['Given', 'Held', 'Refused', 'NotAvailable'] as const;

export const MAR_STATUS_LABELS: Record<string, string> = {
  Given: 'Given',
  Held: 'Held',
  Refused: 'Refused',
  NotAvailable: 'Not Available',
};

export const MAR_STATUS_COLORS: Record<string, string> = {
  Given: 'bg-green-100 text-green-800',
  Held: 'bg-yellow-100 text-yellow-800',
  Refused: 'bg-red-100 text-red-800',
  NotAvailable: 'bg-gray-100 text-gray-600',
};

export const ADMIN_ROUTES = ['Oral', 'IV', 'IM', 'SC', 'Topical', 'Inhaled', 'PR', 'SL'] as const;
