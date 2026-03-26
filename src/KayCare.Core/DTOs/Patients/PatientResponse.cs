namespace KayCare.Core.DTOs.Patients;

public class PatientResponse
{
    public Guid PatientId { get; set; }
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool HasAllergies { get; set; }
    public bool IsActive { get; set; }
    public DateTime RegisteredAt { get; set; }
}
