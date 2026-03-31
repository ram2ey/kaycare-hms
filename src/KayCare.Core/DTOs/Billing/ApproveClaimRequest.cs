using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Billing;

public class ApproveClaimRequest
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Approved amount must be greater than zero.")]
    public decimal ApprovedAmount { get; set; }

    [MaxLength(1000)] public string? Notes { get; set; }
}
