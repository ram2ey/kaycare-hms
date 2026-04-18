using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Billing;

public class CreateCreditNoteRequest
{
    [Required] public Guid BillId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required, MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(1000)] public string? Notes { get; set; }
}
