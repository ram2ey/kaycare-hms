import type { PrescriptionItemRequest } from './prescriptions';

export interface PrescriptionTemplateItemResponse {
  templateItemId: string;
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

export interface PrescriptionTemplateResponse {
  templateId: string;
  name: string;
  description: string | null;
  isShared: boolean;
  createdByName: string;
  itemCount: number;
  createdAt: string;
}

export interface PrescriptionTemplateDetailResponse extends PrescriptionTemplateResponse {
  items: PrescriptionTemplateItemResponse[];
}

export interface CreatePrescriptionTemplateRequest {
  name: string;
  description?: string;
  isShared: boolean;
  items: PrescriptionItemRequest[];
}
