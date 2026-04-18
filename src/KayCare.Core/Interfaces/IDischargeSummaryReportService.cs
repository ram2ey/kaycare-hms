using KayCare.Core.DTOs.Inpatient;

namespace KayCare.Core.Interfaces;

public interface IDischargeSummaryReportService
{
    Task<byte[]> GenerateAsync(DischargeSummaryResponse summary, CancellationToken ct = default);
}
