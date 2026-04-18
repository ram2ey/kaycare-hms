using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Patients;

public class PatientSearchRequest
{
    /// <summary>Searches FirstName, LastName, MRN, and PhoneNumber.</summary>
    [MaxLength(200)]
    public string? Query { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
