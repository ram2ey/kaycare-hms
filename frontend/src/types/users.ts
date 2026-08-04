export interface UserResponse {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  roleId: number;
  role: string;
  phoneNumber: string | null;
  licenseNumber: string | null;
  department: string | null;
  isActive: boolean;
  mustChangePassword: boolean;
  lastLoginAt: string | null;
  createdAt: string;
}

export interface CreateUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  roleId: number;
  password: string;
  phoneNumber?: string;
  licenseNumber?: string;
  department?: string;
}

export interface UpdateUserRequest {
  firstName: string;
  lastName: string;
  roleId: number;
  phoneNumber?: string;
  licenseNumber?: string;
  department?: string;
}

export interface ResetPasswordRequest {
  newPassword: string;
}

export interface DepartmentSummary {
  name: string;
  userCount: number;
}

export const ROLE_OPTIONS = [
  { id: 2, name: 'Admin' },
  { id: 3, name: 'Doctor' },
  { id: 4, name: 'Nurse' },
  { id: 5, name: 'Receptionist' },
  { id: 6, name: 'Pharmacist' },
  { id: 7, name: 'LabTechnician' },
  { id: 8, name: 'BillingOfficer' },
  { id: 9, name: 'PharmacyManager' },
  { id: 10, name: 'BillingManager' },
  { id: 11, name: 'LabManager' },
  { id: 12, name: 'RadiologyManager' },
  { id: 13, name: 'NurseManager' },
] as const;

export const ROLE_COLORS: Record<string, string> = {
  SuperAdmin:       'bg-purple-100 text-purple-700',
  Admin:            'bg-blue-100 text-blue-700',
  Doctor:           'bg-green-100 text-green-700',
  Nurse:            'bg-teal-100 text-teal-700',
  Receptionist:     'bg-yellow-100 text-yellow-700',
  Pharmacist:       'bg-orange-100 text-orange-700',
  LabTechnician:    'bg-cyan-100 text-cyan-700',
  BillingOfficer:   'bg-rose-100 text-rose-700',
  PharmacyManager:  'bg-amber-100 text-amber-800 font-semibold',
  BillingManager:   'bg-emerald-100 text-emerald-800 font-semibold',
  LabManager:       'bg-indigo-100 text-indigo-800 font-semibold',
  RadiologyManager: 'bg-violet-100 text-violet-800 font-semibold',
  NurseManager:     'bg-sky-100 text-sky-800 font-semibold',
};
