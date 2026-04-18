export interface AppointmentResponse {
  appointmentId: string;
  patientId: string;
  patientName: string;
  medicalRecordNumber: string;
  doctorUserId: string;
  doctorName: string;
  scheduledAt: string;
  durationMinutes: number;
  appointmentType: string;
  status: string;
  chiefComplaint: string | null;
  room: string | null;
}

export interface AppointmentDetailResponse extends AppointmentResponse {
  notes: string | null;
  cancelledAt: string | null;
  cancellationReason: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateAppointmentRequest {
  patientId: string;
  doctorUserId: string;
  scheduledAt: string;
  durationMinutes: number;
  appointmentType: string;
  chiefComplaint?: string;
  room?: string;
  notes?: string;
}

export interface CalendarRequest {
  doctorUserId?: string;
  from?: string;
  to?: string;
  status?: string;
}

export interface UserSummary {
  userId: string;
  fullName: string;
  email: string;
  role: string;
  department: string | null;
  licenseNumber: string | null;
}

export const AppointmentStatuses = [
  'Scheduled', 'Confirmed', 'CheckedIn', 'InProgress', 'Completed', 'Cancelled', 'NoShow',
] as const;

export const AppointmentTypes = [
  'Consultation', 'FollowUp', 'Procedure', 'Emergency', 'Checkup', 'Vaccination', 'LabVisit',
] as const;

// Status → allowed next transitions (mirrors backend)
export const StatusTransitions: Record<string, string[]> = {
  Scheduled:  ['Confirmed', 'Cancelled', 'NoShow'],
  Confirmed:  ['CheckedIn', 'Cancelled', 'NoShow'],
  CheckedIn:  ['InProgress', 'Cancelled'],
  InProgress: ['Completed'],
  Completed:  [],
  Cancelled:  [],
  NoShow:     [],
};

export const STATUS_COLORS: Record<string, string> = {
  Scheduled:  'bg-blue-100 text-blue-700',
  Confirmed:  'bg-indigo-100 text-indigo-700',
  CheckedIn:  'bg-yellow-100 text-yellow-700',
  InProgress: 'bg-orange-100 text-orange-700',
  Completed:  'bg-green-100 text-green-700',
  Cancelled:  'bg-gray-100 text-gray-500',
  NoShow:     'bg-red-100 text-red-600',
};
