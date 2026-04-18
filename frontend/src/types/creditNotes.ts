export const CREDIT_NOTE_STATUSES = ['Draft', 'Approved', 'Applied', 'Voided'] as const;
export type CreditNoteStatus = typeof CREDIT_NOTE_STATUSES[number];

export const CREDIT_NOTE_STATUS_LABELS: Record<string, string> = {
  Draft:    'Draft',
  Approved: 'Approved',
  Applied:  'Applied',
  Voided:   'Voided',
};

export interface CreditNoteResponse {
  creditNoteId:     string;
  creditNoteNumber: string;
  billId:           string;
  billNumber:       string;
  patientId:        string;
  patientName:      string;
  patientMrn:       string;
  amount:           number;
  reason:           string;
  status:           string;
  notes:            string | null;
  createdByName:    string;
  approvedByName:   string | null;
  approvedAt:       string | null;
  appliedAt:        string | null;
  createdAt:        string;
  updatedAt:        string;
}

export interface CreateCreditNoteRequest {
  billId:  string;
  amount:  number;
  reason:  string;
  notes?:  string;
}
