export interface DiagnosisDto {
  code: string;
  description: string;
}

export interface ConsultationSummaryResponse {
  consultationId: string;
  appointmentId: string;
  patientId: string;
  patientName: string;
  medicalRecordNumber: string;
  doctorUserId: string;
  doctorName: string;
  primaryDiagnosisCode: string | null;
  primaryDiagnosisDesc: string | null;
  status: string;
  signedAt: string | null;
  createdAt: string;
}

export interface ConsultationDetailResponse extends ConsultationSummaryResponse {
  subjectiveNotes: string | null;
  objectiveNotes: string | null;
  assessmentNotes: string | null;
  planNotes: string | null;
  bloodPressureSystolic: number | null;
  bloodPressureDiastolic: number | null;
  heartRateBPM: number | null;
  temperatureCelsius: number | null;
  weightKg: number | null;
  heightCm: number | null;
  oxygenSaturationPct: number | null;
  secondaryDiagnoses: DiagnosisDto[];
  updatedAt: string;
}

export interface CreateConsultationRequest {
  appointmentId: string;
  subjectiveNotes?: string;
  objectiveNotes?: string;
  assessmentNotes?: string;
  planNotes?: string;
}

export interface UpdateConsultationRequest {
  subjectiveNotes?: string;
  objectiveNotes?: string;
  assessmentNotes?: string;
  planNotes?: string;
  bloodPressureSystolic?: number | null;
  bloodPressureDiastolic?: number | null;
  heartRateBPM?: number | null;
  temperatureCelsius?: number | null;
  weightKg?: number | null;
  heightCm?: number | null;
  oxygenSaturationPct?: number | null;
  primaryDiagnosisCode?: string;
  primaryDiagnosisDesc?: string;
  secondaryDiagnoses?: DiagnosisDto[];
}
