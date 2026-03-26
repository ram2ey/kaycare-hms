using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Patients;

public class UpdatePatientRequest
{
    [MaxLength(100)] public string? FirstName { get; set; }
    [MaxLength(100)] public string? MiddleName { get; set; }
    [MaxLength(100)] public string? LastName { get; set; }

    [MaxLength(5)]   public string? BloodType { get; set; }
    [MaxLength(50)]  public string? NationalId { get; set; }

    [EmailAddress, MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(20)]  public string? PhoneNumber { get; set; }
    [MaxLength(20)]  public string? AlternatePhone { get; set; }

    [MaxLength(200)] public string? AddressLine1 { get; set; }
    [MaxLength(200)] public string? AddressLine2 { get; set; }
    [MaxLength(100)] public string? City { get; set; }
    [MaxLength(100)] public string? State { get; set; }
    [MaxLength(20)]  public string? PostalCode { get; set; }
    [MaxLength(100)] public string? Country { get; set; }

    [MaxLength(200)] public string? EmergencyContactName { get; set; }
    [MaxLength(20)]  public string? EmergencyContactPhone { get; set; }
    [MaxLength(50)]  public string? EmergencyContactRelation { get; set; }

    [MaxLength(200)] public string? InsuranceProvider { get; set; }
    [MaxLength(100)] public string? InsurancePolicyNumber { get; set; }
    [MaxLength(100)] public string? InsuranceGroupNumber { get; set; }

    public bool? HasChronicConditions { get; set; }
}
