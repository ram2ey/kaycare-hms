export interface TenantResponse {
  tenantId: string;
  tenantCode: string;
  tenantName: string;
  subdomain: string;
  subscriptionPlan: string;
  isActive: boolean;
  maxUsers: number;
  storageQuotaGB: number;
  userCount: number;
  isAiEnabled: boolean;
  aiMonthlyQuota: number;
  aiRequestsThisMonth: number;
  allowedAiTiers: string;
  hasCustomOpenRouterKey: boolean;
  createdAt: string;
}

export interface CreateTenantResponse extends TenantResponse {
  /** Only present on the response to tenant creation — shown to the caller once. */
  temporaryPassword: string;
}

export interface CreateTenantRequest {
  tenantCode: string;
  tenantName: string;
  subscriptionPlan: string;
  maxUsers: number;
  storageQuotaGB: number;
  isAiEnabled: boolean;
  aiMonthlyQuota: number;
  allowedAiTiers: string;
  customOpenRouterKey?: string;
  adminEmail: string;
  adminFirstName: string;
  adminLastName: string;
}

export interface UpdateTenantRequest {
  tenantName: string;
  subscriptionPlan: string;
  maxUsers: number;
  storageQuotaGB: number;
  isAiEnabled: boolean;
  aiMonthlyQuota: number;
  allowedAiTiers: string;
  customOpenRouterKey?: string;
}

export const SUBSCRIPTION_PLANS = ['Standard', 'Professional', 'Enterprise'];
export const AI_TIERS = ['Standard', 'Pro', 'Enterprise'];
