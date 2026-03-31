import apiClient from './client';
import type { CreditNoteResponse, CreateCreditNoteRequest } from '../types/creditNotes';

export const getCreditNotes = (params?: { status?: string; billId?: string; patientId?: string }) =>
  apiClient.get<CreditNoteResponse[]>('/credit-notes', { params }).then((r) => r.data);

export const getCreditNote = (id: string) =>
  apiClient.get<CreditNoteResponse>(`/credit-notes/${id}`).then((r) => r.data);

export const createCreditNote = (data: CreateCreditNoteRequest) =>
  apiClient.post<CreditNoteResponse>('/credit-notes', data).then((r) => r.data);

export const approveCreditNote = (id: string) =>
  apiClient.put<CreditNoteResponse>(`/credit-notes/${id}/approve`).then((r) => r.data);

export const applyCreditNote = (id: string) =>
  apiClient.put<CreditNoteResponse>(`/credit-notes/${id}/apply`).then((r) => r.data);

export const voidCreditNote = (id: string) =>
  apiClient.put<CreditNoteResponse>(`/credit-notes/${id}/void`).then((r) => r.data);
