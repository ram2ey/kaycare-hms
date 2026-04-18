using KayCare.Core.DTOs.Pharmacy;

namespace KayCare.Core.Interfaces;

public interface ICSRegisterReportService
{
    Task<byte[]> GenerateAsync(List<CSRegisterDrugEntry> entries, DateOnly? from, DateOnly? to, CancellationToken ct);
}
