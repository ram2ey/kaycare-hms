using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.Validation;

/// <summary>
/// Requires at least 3 of the 4 character classes (upper, lower, digit, symbol) on top of the
/// existing [MinLength(8)] this is paired with everywhere it's used. Deliberately not a full
/// zxcvbn-style strength estimate — just enough to rule out "password1"-class weak passwords
/// without frustrating users with an opaque strength meter.
/// </summary>
public class StrongPasswordAttribute : ValidationAttribute
{
    public StrongPasswordAttribute()
        : base("{0} must contain at least 3 of: uppercase letter, lowercase letter, digit, symbol.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is not string password || password.Length == 0)
        {
            // Presence/length is [Required]/[MinLength]'s job, not this attribute's.
            return true;
        }

        var classes = 0;
        if (password.Any(char.IsUpper)) classes++;
        if (password.Any(char.IsLower)) classes++;
        if (password.Any(char.IsDigit)) classes++;
        if (password.Any(c => !char.IsLetterOrDigit(c))) classes++;

        return classes >= 3;
    }
}
