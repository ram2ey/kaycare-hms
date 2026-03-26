// Auth
export interface LoginRequest {
  tenantCode: string;
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  email: string;
  fullName: string;
  role: string;
  mustChangePassword: boolean;
}

export interface AuthUser {
  token: string;
  email: string;
  fullName: string;
  role: string;
  mustChangePassword: boolean;
  tenantCode: string;
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
  SuperAdmin: 'SuperAdmin',
  Admin: 'Admin',
  Doctor: 'Doctor',
  Nurse: 'Nurse',
  Receptionist: 'Receptionist',
  Pharmacist: 'Pharmacist',
} as const;

export type Role = (typeof Roles)[keyof typeof Roles];
