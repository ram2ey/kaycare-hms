export interface BillItemRequest {
  description: string;
  category?: string;
  quantity: number;
  unitPrice: number;
}

export interface BillItemResponse {
  itemId: string;
  description: string;
  category: string | null;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  sourceType: string | null;
  sourceId: string | null;
}

export interface PaymentResponse {
  paymentId: string;
  amount: number;
  paymentMethod: string;
  reference: string | null;
  receivedByName: string;
  paymentDate: string;
  notes: string | null;
  createdAt: string;
}

export interface BillResponse {
  billId: string;
  billNumber: string;
  patientId: string;
  patientName: string;
  medicalRecordNumber: string;
  status: string;
  totalAmount: number;
  discountAmount: number;
  paidAmount: number;
  balanceDue: number;
  issuedAt: string | null;
  createdAt: string;
}

export interface BillDetailResponse extends BillResponse {
  consultationId: string | null;
  payerId: string | null;
  payerName: string | null;
  discountReason: string | null;
  createdByName: string;
  notes: string | null;
  updatedAt: string;
  items: BillItemResponse[];
  payments: PaymentResponse[];
}

export interface CreateBillRequest {
  patientId: string;
  consultationId?: string;
  payerId?: string;
  discountAmount?: number;
  discountReason?: string;
  notes?: string;
  items: BillItemRequest[];
}

export interface AddPaymentRequest {
  amount: number;
  paymentMethod: string;
  reference?: string;
  notes?: string;
}

export const STATUS_COLORS: Record<string, string> = {
  Draft:         'bg-gray-100 text-gray-600',
  Issued:        'bg-blue-100 text-blue-700',
  PartiallyPaid: 'bg-yellow-100 text-yellow-700',
  Paid:          'bg-green-100 text-green-700',
  Cancelled:     'bg-gray-100 text-gray-400',
  Void:          'bg-red-100 text-red-500',
};

export const PAYMENT_METHODS = [
  'Cash', 'MobileMoney', 'Card', 'Insurance', 'BankTransfer', 'Cheque', 'Other',
];

export const BILL_CATEGORIES = [
  'Consultation', 'Procedure', 'Medication', 'Laboratory', 'Imaging',
  'Nursing', 'Bed', 'Surgery', 'Other',
];
