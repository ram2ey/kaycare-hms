export interface AuditLogResponse {
  auditLogId: number;
  tenantId: string;
  userId: string;
  userEmail: string;
  action: string;
  entityType: string;
  entityId: string;
  patientId: string | null;
  details: string | null;
  ipAddress: string | null;
  timestamp: string;
}

export interface AuditLogQueryRequest {
  patientId?: string;
  userId?: string;
  action?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

export const AUDIT_ACTIONS = [
  'Patient.View', 'Patient.Create', 'Patient.Update',
];
