import { TenantType } from './index';

export interface TenantResponse {
  tenantId: string;
  tenantCode: string;
  tenantName: string;
  subdomain: string;
  tenantType: string;
  subscriptionPlan: string;
  isActive: boolean;
  maxUsers: number;
  storageQuotaGB: number;
  userCount: number;
  createdAt: string;
}

export interface CreateTenantRequest {
  tenantCode: string;
  tenantName: string;
  tenantType: string;
  subscriptionPlan: string;
  maxUsers: number;
  storageQuotaGB: number;
  adminEmail: string;
  adminFirstName: string;
  adminLastName: string;
}

export interface UpdateTenantRequest {
  tenantName: string;
  tenantType: string;
  subscriptionPlan: string;
  maxUsers: number;
  storageQuotaGB: number;
}

export const TENANT_TYPE_OPTIONS = [
  { value: TenantType.HMS,     label: 'KayCare HMS — Hospital / Clinic' },
  { value: TenantType.PharmOS, label: 'PharmOS — Standalone Pharmacy' },
  { value: TenantType.LIS,     label: 'LIS — Standalone Laboratory' },
];

export const TENANT_TYPE_COLORS: Record<string, string> = {
  HMS:     'bg-blue-100 text-blue-700',
  PharmOS: 'bg-green-100 text-green-700',
  LIS:     'bg-purple-100 text-purple-700',
};

export const SUBSCRIPTION_PLANS = ['Standard', 'Professional', 'Enterprise'];
