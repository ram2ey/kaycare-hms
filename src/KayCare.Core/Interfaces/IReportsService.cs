using KayCare.Core.DTOs.Reports;

namespace KayCare.Core.Interfaces;

public interface IReportsService
{
    Task<ClinicalReportResponse>  GetClinicalAsync(DateOnly from, DateOnly to, CancellationToken ct);
    Task<InpatientReportResponse> GetInpatientAsync(DateOnly from, DateOnly to, CancellationToken ct);
    Task<LabReportResponse>       GetLabAsync(DateOnly from, DateOnly to, CancellationToken ct);
    Task<ReferralReportResponse>  GetReferralsAsync(DateOnly from, DateOnly to, CancellationToken ct);
    Task<PharmacyReportResponse>  GetPharmacyAsync(DateOnly from, DateOnly to, CancellationToken ct);
}
