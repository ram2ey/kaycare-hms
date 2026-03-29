using System.ComponentModel.DataAnnotations;
using KayCare.Core.Constants;

namespace KayCare.Core.DTOs.Billing;

public class SavePayerRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? ContactPhone { get; set; }

    [EmailAddress, MaxLength(256)]
    public string? ContactEmail { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
}
