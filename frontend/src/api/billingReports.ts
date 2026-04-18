import apiClient from './client';
import type { ArAgingReport, RevenueDashboardResponse } from '../types/billingReports';

export const getArAging = () =>
  apiClient.get<ArAgingReport>('/billing-reports/ar-aging').then((r) => r.data);

export const getRevenueDashboard = () =>
  apiClient.get<RevenueDashboardResponse>('/billing-reports/revenue-dashboard').then((r) => r.data);
