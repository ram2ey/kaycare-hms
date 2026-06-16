export interface LabTestCatalog {
  labTestCatalogId: string;
  testCode: string;
  testName: string;
  department: string;
  instrumentType: string | null;
  isManualEntry: boolean;
  tatHours: number;
  defaultUnit: string | null;
  defaultReferenceRange: string | null;
}

export interface LabOrderItem {
  labOrderItemId: string;
  labTestCatalogId: string;
  testName: string;
  department: string;
  instrumentType: string | null;
  isManualEntry: boolean;
  tatHours: number;
  accessionNumber: string | null;
  status: LabOrderItemStatus;
  sampleReceivedAt: string | null;
  resultedAt: string | null;
  signedAt: string | null;
  manualResult: string | null;
  manualResultNotes: string | null;
  manualResultUnit: string | null;
  manualResultReferenceRange: string | null;
  manualResultFlag: string | null;
  labResultId: string | null;
  isTatExceeded: boolean;
}

export interface LabOrder {
  labOrderId: string;
  patientId: string;
  patientName: string;
  patientMrn: string;
  patientGender: string;
  patientDob: string;
  consultationId: string | null;
  billId: string | null;
  billNumber: string | null;
  orderingDoctorUserId: string;
  orderingDoctorName: string;
  organisation: string;
  status: LabOrderStatus;
  notes: string | null;
  orderedAt: string;
  incompleteCount: number;
  completedCount: number;
  signedCount: number;
  testNames: string[];
}

export interface LabOrderDetail extends LabOrder {
  items: LabOrderItemResponse[];
}

export type LabOrderStatus =
  | 'Pending'
  | 'Active'
  | 'PartiallyCompleted'
  | 'Completed'
  | 'Signed';

export type LabOrderItemStatus =
  | 'Ordered'
  | 'SampleReceived'
  | 'Resulted'
  | 'Signed';

export const ITEM_STATUS_COLORS: Record<LabOrderItemStatus, string> = {
  Ordered:        'bg-gray-100 text-gray-700',
  SampleReceived: 'bg-blue-100 text-blue-700',
  Resulted:       'bg-green-100 text-green-700',
  Signed:         'bg-purple-100 text-purple-700',
};

export const ORDER_STATUS_COLORS: Record<LabOrderStatus, string> = {
  Pending:            'bg-gray-100 text-gray-700',
  Active:             'bg-blue-100 text-blue-700',
  PartiallyCompleted: 'bg-yellow-100 text-yellow-700',
  Completed:          'bg-green-100 text-green-700',
  Signed:             'bg-purple-100 text-purple-700',
};

export const DEPARTMENTS = [
  'Haematology',
  'Chemistry',
  'Immunology',
  'Serology',
  'Urinalysis',
  'Microbiology',
] as const;

export const FLAG_COLORS: Record<string, string> = {
  H: 'bg-red-100 text-red-700 font-semibold',
  L: 'bg-blue-100 text-blue-700 font-semibold',
  HH: 'bg-red-200 text-red-800 font-bold border border-red-300 animate-pulse',
  LL: 'bg-blue-200 text-blue-800 font-bold border border-blue-300 animate-pulse',
  A: 'bg-yellow-100 text-yellow-700 font-semibold',
  N: 'bg-gray-100 text-gray-700',
  CRIT: 'bg-red-200 text-red-850 font-extrabold border border-red-400 animate-pulse',
};

export interface LabOrderItemResponse extends LabOrderItem {
  isCritical?: boolean;
  criticalCallLogId?: string | null;
  criticalCallLogRecipient?: string | null;
  criticalCallLogNotes?: string | null;
  criticalCallLogCalledAt?: string | null;
  patientName?: string | null;
  patientMrn?: string | null;
  hl7ResultValue?: string | null;
  hl7ResultUnit?: string | null;
  hl7Flag?: string | null;
  hl7ResultReferenceRange?: string | null;
}
