namespace KayCare.Core.Interfaces;

public interface IBillingPdfService
{
    /// <summary>Generates an A4 invoice PDF for the bill. Returns null if not found.</summary>
    Task<byte[]?> GenerateBillInvoiceAsync(Guid billId, CancellationToken ct);

    /// <summary>Generates a payment receipt PDF. Returns null if not found.</summary>
    Task<byte[]?> GeneratePaymentReceiptAsync(Guid paymentId, CancellationToken ct);
}
