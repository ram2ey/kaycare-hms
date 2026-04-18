namespace KayCare.Core.Interfaces;

public interface IPrescriptionReportService
{
    /// <summary>Generates a PDF prescription slip. Returns null if the prescription is not found.</summary>
    Task<byte[]?> GeneratePrescriptionReportAsync(Guid prescriptionId, CancellationToken ct);
}
