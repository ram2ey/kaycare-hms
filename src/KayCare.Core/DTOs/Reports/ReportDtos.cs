namespace KayCare.Core.DTOs.Reports;

// ── Shared ────────────────────────────────────────────────────────────────────

public record DailyCount(string Date, int Count);
public record NameCount(string Name, int Count);

// ── Clinical ──────────────────────────────────────────────────────────────────

public record ClinicalReportResponse(
    int  TotalConsultations,
    int  SignedConsultations,
    int  TotalAppointments,
    int  AttendedAppointments,
    int  NewPatients,
    IReadOnlyList<DailyCount> ConsultationsByDay,
    IReadOnlyList<NameCount>  TopDiagnoses,
    IReadOnlyList<NameCount>  AppointmentsByStatus
);

// ── Inpatient ────────────────────────────────────────────────────────────────

public record InpatientReportResponse(
    int     TotalAdmissions,
    int     ActiveAdmissions,
    int     Discharges,
    decimal AverageLengthOfStayDays,
    int     TotalBeds,
    int     OccupiedBeds,
    decimal OccupancyRate,
    IReadOnlyList<DailyCount> AdmissionsByDay,
    IReadOnlyList<NameCount>  AdmissionsByWard,
    IReadOnlyList<NameCount>  DischargesByType
);

// ── Lab ───────────────────────────────────────────────────────────────────────

public record LabReportResponse(
    int TotalOrders,
    int CompletedOrders,
    int PendingOrders,
    IReadOnlyList<DailyCount> OrdersByDay,
    IReadOnlyList<NameCount>  TopTests,
    IReadOnlyList<NameCount>  OrdersByStatus
);

// ── Referrals ────────────────────────────────────────────────────────────────

public record ReferralReportResponse(
    int TotalReferrals,
    int InternalReferrals,
    int ExternalReferrals,
    int AcceptedReferrals,
    int DeclinedReferrals,
    int PendingReferrals,
    IReadOnlyList<NameCount> ByStatus,
    IReadOnlyList<NameCount> ByUrgency,
    IReadOnlyList<NameCount> TopReferringDoctors
);

// ── Pharmacy ─────────────────────────────────────────────────────────────────

public record PharmacyReportResponse(
    int TotalDispenseEvents,
    int LowStockItems,
    int OutOfStockItems,
    int TotalDrugs,
    IReadOnlyList<NameCount> TopDispensedDrugs,
    IReadOnlyList<NameCount> StockByCategory
);
