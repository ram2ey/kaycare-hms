import apiClient from './client';
import type { AuditLogQueryRequest } from '../types/audit';
import type { PagedResult } from '../types';
import type { AuditLogResponse } from '../types/audit';

export const queryAuditLogs = (params: AuditLogQueryRequest) =>
  apiClient.get<PagedResult<AuditLogResponse>>('/audit-logs', { params }).then((r) => r.data);
