export interface DailyCount { date: string; count: number; }
export interface NameCount  { name: string; count: number; }

export interface ClinicalReportResponse {
  totalConsultations: number;
  signedConsultations: number;
  totalAppointments: number;
  attendedAppointments: number;
  newPatients: number;
  consultationsByDay: DailyCount[];
  topDiagnoses: NameCount[];
  appointmentsByStatus: NameCount[];
}

export interface InpatientReportResponse {
  totalAdmissions: number;
  activeAdmissions: number;
  discharges: number;
  averageLengthOfStayDays: number;
  totalBeds: number;
  occupiedBeds: number;
  occupancyRate: number;
  admissionsByDay: DailyCount[];
  admissionsByWard: NameCount[];
  dischargesByType: NameCount[];
}

export interface LabReportResponse {
  totalOrders: number;
  completedOrders: number;
  pendingOrders: number;
  ordersByDay: DailyCount[];
  topTests: NameCount[];
  ordersByStatus: NameCount[];
}

export interface ReferralReportResponse {
  totalReferrals: number;
  internalReferrals: number;
  externalReferrals: number;
  acceptedReferrals: number;
  declinedReferrals: number;
  pendingReferrals: number;
  byStatus: NameCount[];
  byUrgency: NameCount[];
  topReferringDoctors: NameCount[];
}

export interface PharmacyReportResponse {
  totalDispenseEvents: number;
  lowStockItems: number;
  outOfStockItems: number;
  totalDrugs: number;
  topDispensedDrugs: NameCount[];
  stockByCategory: NameCount[];
}
