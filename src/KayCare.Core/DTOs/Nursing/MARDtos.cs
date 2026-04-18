namespace KayCare.Core.DTOs.Nursing;

public record RecordAdministrationRequest(
    Guid    PrescriptionId,
    Guid    PrescriptionItemId,
    Guid    PatientId,
    Guid?   AdmissionId,
    string  Status,
    string? DoseGiven,
    string? Route,
    string? Notes
);

public record MAREntryResponse(
    Guid     MedicationAdministrationId,
    Guid     PrescriptionId,
    Guid     PrescriptionItemId,
    string   MedicationName,
    string   Dosage,
    string   Frequency,
    string   AdministeredByName,
    DateTime AdministeredAt,
    string   Status,
    string?  DoseGiven,
    string?  Route,
    string?  Notes
);

public record MARItemResponse(
    Guid   PrescriptionItemId,
    Guid   PrescriptionId,
    Guid   PatientId,
    string MedicationName,
    string Dosage,
    string Frequency,
    string Instructions,
    List<MAREntryResponse> Administrations
);
