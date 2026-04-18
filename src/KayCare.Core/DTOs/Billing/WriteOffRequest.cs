using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Billing;

public class WriteOffRequest
{
    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
