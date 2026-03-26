namespace MediCloud.Core.Interfaces;

public interface ILabReportService
{
    /// <summary>
    /// Generates a PDF lab report for the given lab order.
    /// Returns the PDF as a byte array, or null if the order is not found.
    /// </summary>
    Task<byte[]?> GenerateLabOrderReportAsync(Guid labOrderId, CancellationToken ct);
}
