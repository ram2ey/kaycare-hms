using System.ComponentModel.DataAnnotations;
using KayCare.Core.Validation;

namespace KayCare.Core.DTOs.Auth;

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(8), StrongPassword]
    public string NewPassword { get; set; } = string.Empty;
}
