// Auth
export interface LoginRequest {
  tenantCode: string;
  email: string;
  password: string;
}

export interface LoginResponse {
  csrfToken: string;
  email: string;
  fullName: string;
  role: string;
  tenantCode: string;
  mustChangePassword: boolean;
}

export interface MeResponse extends LoginResponse {
  userId: string;
}

export interface AuthUser {
  email: string;
  fullName: string;
  role: string;
  tenantCode: string;
  mustChangePassword: boolean;
}

// Common
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// Roles
export const Roles = {
  SuperAdmin:     'SuperAdmin',
  Admin:          'Admin',
  Doctor:         'Doctor',
  Nurse:          'Nurse',
  Receptionist:   'Receptionist',
  Pharmacist:     'Pharmacist',
  LabTechnician:  'LabTechnician',
  BillingOfficer: 'BillingOfficer',
  PharmacyManager: 'PharmacyManager',
  BillingManager:  'BillingManager',
  LabManager:      'LabManager',
  RadiologyManager: 'RadiologyManager',
  NurseManager:    'NurseManager',
} as const;

export type Role = (typeof Roles)[keyof typeof Roles];

// Tenant types
export const TenantType = {
  HMS:     'HMS',
  PharmOS: 'PharmOS',
  LIS:     'LIS',
} as const;
