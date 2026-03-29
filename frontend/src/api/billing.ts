import apiClient from './client';
import type {
  BillResponse,
  BillDetailResponse,
  CreateBillRequest,
  AddPaymentRequest,
  ApplyDiscountRequest,
} from '../types/billing';

export const getOutstanding = () =>
  apiClient.get<BillResponse[]>('/bills/outstanding').then((r) => r.data);

export const getPatientBills = (patientId: string) =>
  apiClient.get<BillResponse[]>(`/bills/patient/${patientId}`).then((r) => r.data);

export const getBill = (id: string) =>
  apiClient.get<BillDetailResponse>(`/bills/${id}`).then((r) => r.data);

export const createBill = (data: CreateBillRequest) =>
  apiClient.post<BillDetailResponse>('/bills', data).then((r) => r.data);

export const issueBill = (id: string) =>
  apiClient.post<BillDetailResponse>(`/bills/${id}/issue`).then((r) => r.data);

export const addPayment = (id: string, data: AddPaymentRequest) =>
  apiClient.post<BillDetailResponse>(`/bills/${id}/payments`, data).then((r) => r.data);

export const cancelBill = (id: string) =>
  apiClient.post<BillDetailResponse>(`/bills/${id}/cancel`).then((r) => r.data);

export const voidBill = (id: string) =>
  apiClient.post<BillDetailResponse>(`/bills/${id}/void`).then((r) => r.data);

export const applyDiscount = (id: string, data: ApplyDiscountRequest) =>
  apiClient.patch<BillDetailResponse>(`/bills/${id}/discount`, data).then((r) => r.data);

export const downloadInvoice = (id: string) =>
  apiClient.get(`/bills/${id}/report`, { responseType: 'blob' }).then((r) => r.data as Blob);

export const downloadReceipt = (paymentId: string) =>
  apiClient.get(`/bills/payments/${paymentId}/receipt`, { responseType: 'blob' }).then((r) => r.data as Blob);
