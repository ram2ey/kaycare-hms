using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Billing;

public class CreateClaimRequest
{
    [Required] public Guid    BillId      { get; set; }
    [Required] public Guid    PayerId     { get; set; }

    /// <summary>Override claim amount — defaults to bill's BalanceDue if not provided.</summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Claim amount must be greater than zero.")]
    public decimal? ClaimAmount { get; set; }

    [MaxLength(1000)] public string? Notes { get; set; }
}
