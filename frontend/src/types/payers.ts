export const PAYER_TYPES = ['NHIS', 'PrivateInsurance', 'Corporate', 'Government'] as const;
export type PayerType = typeof PAYER_TYPES[number];

export const PAYER_TYPE_LABELS: Record<string, string> = {
  NHIS:             'NHIS',
  PrivateInsurance: 'Private Insurance',
  Corporate:        'Corporate',
  Government:       'Government',
};

export interface PayerResponse {
  payerId:      string;
  name:         string;
  type:         string;
  contactPhone: string | null;
  contactEmail: string | null;
  notes:        string | null;
  isActive:     boolean;
  createdAt:    string;
  updatedAt:    string;
}

export interface SavePayerRequest {
  name:         string;
  type:         string;
  contactPhone?: string;
  contactEmail?: string;
  notes?:        string;
  isActive:     boolean;
}
