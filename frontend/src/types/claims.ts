export const CLAIM_STATUSES = ['Draft', 'Submitted', 'Approved', 'PartiallyApproved', 'Rejected', 'Cancelled'] as const;
export type ClaimStatus = typeof CLAIM_STATUSES[number];

export const CLAIM_STATUS_LABELS: Record<string, string> = {
  Draft:             'Draft',
  Submitted:         'Submitted',
  Approved:          'Approved',
  PartiallyApproved: 'Partially Approved',
  Rejected:          'Rejected',
  Cancelled:         'Cancelled',
};

export interface InsuranceClaimResponse {
  claimId:         string;
  claimNumber:     string;
  billId:          string;
  billNumber:      string;
  payerId:         string;
  payerName:       string;
  payerType:       string;
  patientId:       string;
  patientName:     string;
  patientMrn:      string;
  nhisNumber:      string | null;
  status:          string;
  claimAmount:     number;
  approvedAmount:  number | null;
  rejectionReason: string | null;
  notes:           string | null;
  submittedAt:     string | null;
  responseAt:      string | null;
  paymentId:       string | null;
  createdByName:   string;
  createdAt:       string;
  updatedAt:       string;
}

export interface CreateClaimRequest {
  billId:       string;
  payerId:      string;
  claimAmount?: number;
  notes?:       string;
}

export interface ApproveClaimRequest {
  approvedAmount: number;
  notes?:         string;
}

export interface RejectClaimRequest {
  rejectionReason: string;
  notes?:          string;
}
