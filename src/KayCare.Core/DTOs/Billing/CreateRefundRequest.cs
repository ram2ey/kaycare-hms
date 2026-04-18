using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Billing;

public class CreateRefundRequest
{
    [Required] public Guid BillId { get; set; }

    /// <summary>Optional — link refund to a specific credit note.</summary>
    public Guid? CreditNoteId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required, MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string RefundMethod { get; set; } = string.Empty;

    [MaxLength(200)] public string? Reference { get; set; }
    [MaxLength(1000)] public string? Notes    { get; set; }
}
