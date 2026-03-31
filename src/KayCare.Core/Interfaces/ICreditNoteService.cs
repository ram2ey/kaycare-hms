using KayCare.Core.DTOs.Billing;

namespace KayCare.Core.Interfaces;

public interface ICreditNoteService
{
    Task<CreditNoteResponse>       CreateAsync(CreateCreditNoteRequest request, CancellationToken ct = default);
    Task<List<CreditNoteResponse>> GetAllAsync(string? status, Guid? billId, Guid? patientId, CancellationToken ct = default);
    Task<CreditNoteResponse?>      GetByIdAsync(Guid creditNoteId, CancellationToken ct = default);
    Task<CreditNoteResponse>       ApproveAsync(Guid creditNoteId, CancellationToken ct = default);
    Task<CreditNoteResponse>       ApplyAsync(Guid creditNoteId, CancellationToken ct = default);
    Task<CreditNoteResponse>       VoidAsync(Guid creditNoteId, CancellationToken ct = default);
}
