export const REFUND_STATUSES = ['Pending', 'Processed', 'Cancelled'] as const;
export type RefundStatus = typeof REFUND_STATUSES[number];

export const REFUND_METHODS = ['Cash', 'BankTransfer', 'MobileMoney', 'Cheque'] as const;

export const REFUND_METHOD_LABELS: Record<string, string> = {
  Cash:         'Cash',
  BankTransfer: 'Bank Transfer',
  MobileMoney:  'Mobile Money',
  Cheque:       'Cheque',
};

export interface RefundResponse {
  refundId:         string;
  refundNumber:     string;
  billId:           string;
  billNumber:       string;
  patientId:        string;
  patientName:      string;
  patientMrn:       string;
  creditNoteId:     string | null;
  creditNoteNumber: string | null;
  amount:           number;
  reason:           string;
  refundMethod:     string;
  reference:        string | null;
  status:           string;
  notes:            string | null;
  createdByName:    string;
  processedByName:  string | null;
  processedAt:      string | null;
  createdAt:        string;
  updatedAt:        string;
}

export interface CreateRefundRequest {
  billId:        string;
  creditNoteId?: string;
  amount:        number;
  reason:        string;
  refundMethod:  string;
  reference?:    string;
  notes?:        string;
}
