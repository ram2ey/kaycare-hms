namespace KayCare.Core.DTOs.Patients;

public class PatientDetailResponse : PatientResponse
{
    public string? MiddleName { get; set; }
    public string? BloodType { get; set; }
    public string? NationalId { get; set; }
    public string? Email { get; set; }
    public string? AlternatePhone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
    public string? InsuranceProvider { get; set; }
    public string? InsurancePolicyNumber { get; set; }
    public string? InsuranceGroupNumber { get; set; }
    public bool HasChronicConditions { get; set; }
}
