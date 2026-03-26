export interface PatientResponse {
  patientId: string;
  medicalRecordNumber: string;
  fullName: string;
  dateOfBirth: string;
  age: number;
  gender: string;
  phoneNumber: string | null;
  hasAllergies: boolean;
  isActive: boolean;
  registeredAt: string;
}

export interface PatientDetailResponse extends PatientResponse {
  middleName: string | null;
  bloodType: string | null;
  nationalId: string | null;
  email: string | null;
  alternatePhone: string | null;
  addressLine1: string | null;
  addressLine2: string | null;
  city: string | null;
  state: string | null;
  postalCode: string | null;
  country: string | null;
  emergencyContactName: string | null;
  emergencyContactPhone: string | null;
  emergencyContactRelation: string | null;
  insuranceProvider: string | null;
  insurancePolicyNumber: string | null;
  insuranceGroupNumber: string | null;
  hasChronicConditions: boolean;
}

export interface AllergyResponse {
  allergyId: string;
  allergyType: string;
  allergenName: string;
  reaction: string | null;
  severity: string;
  recordedAt: string;
}

export interface CreatePatientRequest {
  firstName: string;
  middleName?: string;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  bloodType?: string;
  nationalId?: string;
  email?: string;
  phoneNumber?: string;
  alternatePhone?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  country?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  emergencyContactRelation?: string;
  insuranceProvider?: string;
  insurancePolicyNumber?: string;
  insuranceGroupNumber?: string;
}

export interface AddAllergyRequest {
  allergyType: string;
  allergenName: string;
  reaction?: string;
  severity: string;
}

export interface PatientSearchRequest {
  query?: string;
  dateOfBirth?: string;
  page?: number;
  pageSize?: number;
}
