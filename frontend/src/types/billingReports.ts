export interface ArAgingRow {
  billId: string;
  billNumber: string;
  patientName: string;
  medicalRecordNumber: string;
  payerName: string | null;
  issuedAt: string;
  daysOutstanding: number;
  agingBucket: string;
  totalAmount: number;
  paidAmount: number;
  balanceDue: number;
  status: string;
}

export interface ArAgingReport {
  totalBalance0To30: number;
  totalBalance31To60: number;
  totalBalance61To90: number;
  totalBalance90Plus: number;
  grandTotalBalance: number;
  rows: ArAgingRow[];
}

export interface MonthlyRevenuePoint {
  month: string;
  invoiced: number;
  collected: number;
}

export interface PayerRevenueRow {
  payerName: string;
  billCount: number;
  invoiced: number;
  collected: number;
  outstanding: number;
}

export interface StatusCount {
  status: string;
  count: number;
  total: number;
}

export interface RevenueDashboardResponse {
  totalInvoiced: number;
  totalCollected: number;
  totalOutstanding: number;
  totalDiscounts: number;
  totalAdjustments: number;
  totalWrittenOff: number;
  totalBills: number;
  outstandingBills: number;
  overdueBills: number;
  monthlyRevenue: MonthlyRevenuePoint[];
  byPayer: PayerRevenueRow[];
  byStatus: StatusCount[];
}
