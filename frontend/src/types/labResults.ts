export interface LabObservationResponse {
  labObservationId: string;
  sequenceNumber: number;
  testCode: string;
  testName: string;
  value: string | null;
  units: string | null;
  referenceRange: string | null;
  abnormalFlag: string | null;
}

export interface LabResultResponse {
  labResultId: string;
  patientId: string;
  patientMrn: string;
  patientName: string;
  orderingDoctorUserId: string | null;
  orderingDoctorName: string | null;
  accessionNumber: string;
  orderCode: string | null;
  orderName: string | null;
  orderedAt: string | null;
  receivedAt: string;
  status: string;
  observationCount: number;
  createdAt: string;
}

export interface LabResultDetailResponse extends LabResultResponse {
  observations: LabObservationResponse[];
}
