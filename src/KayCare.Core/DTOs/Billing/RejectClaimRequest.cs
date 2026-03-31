using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Billing;

public class RejectClaimRequest
{
    [Required, MaxLength(1000)]
    public string RejectionReason { get; set; } = string.Empty;

    [MaxLength(1000)] public string? Notes { get; set; }
}
