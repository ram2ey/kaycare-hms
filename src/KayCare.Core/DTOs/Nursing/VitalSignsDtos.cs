namespace KayCare.Core.DTOs.Nursing;

public record RecordVitalSignsRequest(
    Guid?   AdmissionId,
    Guid?   ConsultationId,
    int?    BloodPressureSystolic,
    int?    BloodPressureDiastolic,
    int?    PulseRate,
    decimal? Temperature,
    int?    SpO2,
    int?    RespiratoryRate,
    decimal? Weight,
    decimal? Height,
    string? Notes
);

public record VitalSignsResponse(
    Guid     VitalSignsId,
    Guid     PatientId,
    Guid?    AdmissionId,
    Guid?    ConsultationId,
    string   RecordedByName,
    DateTime RecordedAt,
    int?     BloodPressureSystolic,
    int?     BloodPressureDiastolic,
    int?     PulseRate,
    decimal? Temperature,
    int?     SpO2,
    int?     RespiratoryRate,
    decimal? Weight,
    decimal? Height,
    decimal? BMI,
    string?  Notes
);
