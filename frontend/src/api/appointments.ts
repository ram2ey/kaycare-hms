import apiClient from './client';
import type {
  AppointmentResponse,
  AppointmentDetailResponse,
  CreateAppointmentRequest,
  CalendarRequest,
  UserSummary,
} from '../types/appointments';

export const getCalendar = (params: CalendarRequest) =>
  apiClient.get<AppointmentResponse[]>('/appointments/calendar', { params }).then((r) => r.data);

export const getPatientAppointments = (patientId: string) =>
  apiClient.get<AppointmentResponse[]>(`/appointments/patient/${patientId}`).then((r) => r.data);

export const getAppointment = (id: string) =>
  apiClient.get<AppointmentDetailResponse>(`/appointments/${id}`).then((r) => r.data);

export const createAppointment = (data: CreateAppointmentRequest) =>
  apiClient.post<AppointmentDetailResponse>('/appointments', data).then((r) => r.data);

export const confirmAppointment = (id: string) =>
  apiClient.post<AppointmentDetailResponse>(`/appointments/${id}/confirm`).then((r) => r.data);

export const checkInAppointment = (id: string) =>
  apiClient.post<AppointmentDetailResponse>(`/appointments/${id}/check-in`).then((r) => r.data);

export const startAppointment = (id: string) =>
  apiClient.post<AppointmentDetailResponse>(`/appointments/${id}/start`).then((r) => r.data);

export const completeAppointment = (id: string) =>
  apiClient.post<AppointmentDetailResponse>(`/appointments/${id}/complete`).then((r) => r.data);

export const cancelAppointment = (id: string, reason?: string) =>
  apiClient.post<AppointmentDetailResponse>(`/appointments/${id}/cancel`, { reason }).then((r) => r.data);

export const noShowAppointment = (id: string) =>
  apiClient.post<AppointmentDetailResponse>(`/appointments/${id}/no-show`).then((r) => r.data);

export const listDoctors = () =>
  apiClient.get<UserSummary[]>('/users', { params: { role: 'Doctor' } }).then((r) => r.data);
