using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Billing;

public class AddAdjustmentRequest
{
    /// <summary>Positive = extra charge. Negative = credit reduction.</summary>
    [Range(-1_000_000, 1_000_000)]
    public decimal Amount { get; set; }

    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
