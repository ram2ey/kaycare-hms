export interface PrescriptionItemRequest {
  medicationName: string;
  genericName?: string;
  strength: string;
  dosageForm: string;
  frequency: string;
  durationDays: number;
  quantity: number;
  refills: number;
  instructions?: string;
  isControlledSubstance: boolean;
}

export interface PrescriptionItemResponse {
  itemId: string;
  medicationName: string;
  genericName: string | null;
  strength: string;
  dosageForm: string;
  frequency: string;
  durationDays: number;
  quantity: number;
  refills: number;
  instructions: string | null;
  isControlledSubstance: boolean;
}

export interface PrescriptionResponse {
  prescriptionId: string;
  consultationId: string;
  patientId: string;
  patientName: string;
  medicalRecordNumber: string;
  prescribedByUserId: string;
  prescribedByName: string;
  prescriptionDate: string;
  status: string;
  itemCount: number;
  hasControlledSubstances: boolean;
  createdAt: string;
}

export interface PrescriptionDetailResponse extends PrescriptionResponse {
  notes: string | null;
  dispensedAt: string | null;
  dispensedByName: string | null;
  updatedAt: string;
  items: PrescriptionItemResponse[];
}

export interface CreatePrescriptionRequest {
  consultationId: string;
  notes?: string;
  items: PrescriptionItemRequest[];
}

export const STATUS_COLORS: Record<string, string> = {
  Active:     'bg-blue-100 text-blue-700',
  Dispensed:  'bg-green-100 text-green-700',
  Cancelled:  'bg-gray-100 text-gray-500',
};

export const DOSAGE_FORMS = [
  'Tablet', 'Capsule', 'Syrup', 'Suspension', 'Injection', 'Cream', 'Ointment',
  'Drops', 'Inhaler', 'Patch', 'Suppository', 'Other',
];

export const FREQUENCIES = [
  'Once daily', 'Twice daily', 'Three times daily', 'Four times daily',
  'Every 8 hours', 'Every 12 hours', 'Once weekly', 'As needed (PRN)',
  'Before meals', 'After meals', 'At bedtime',
];
