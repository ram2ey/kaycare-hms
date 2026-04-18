namespace KayCare.Core.DTOs.Referrals;

public record CreateReferralRequest(
    Guid      PatientId,
    Guid?     ConsultationId,
    Guid?     ReferredToDoctorUserId,
    string?   ReferredToDepartment,
    string    ReferralType,       // Internal|External
    string?   ExternalFacility,
    string    Urgency,
    string    Reason,
    string?   ClinicalNotes
);

public record UpdateReferralRequest(
    Guid?     ReferredToDoctorUserId,
    string?   ReferredToDepartment,
    string    ReferralType,
    string?   ExternalFacility,
    string    Urgency,
    string    Reason,
    string?   ClinicalNotes
);

public record RespondToReferralRequest(
    string  Action,        // Accept|Complete|Decline
    string? ResponseNotes
);

public record ReferralSummaryResponse(
    Guid    ReferralId,
    string  ReferralNumber,
    string  PatientName,
    string  PatientMrn,
    string  ReferringDoctorName,
    string? ReferredToDoctorName,
    string? ReferredToDepartment,
    string  ReferralType,
    string? ExternalFacility,
    string  Urgency,
    string  Status,
    DateTime CreatedAt
);

public record ReferralDetailResponse(
    Guid    ReferralId,
    string  ReferralNumber,
    Guid    PatientId,
    string  PatientName,
    string  PatientMrn,
    Guid    ReferringDoctorUserId,
    string  ReferringDoctorName,
    Guid?   ReferredToDoctorUserId,
    string? ReferredToDoctorName,
    string? ReferredToDepartment,
    string  ReferralType,
    string? ExternalFacility,
    string  Urgency,
    string  Reason,
    string? ClinicalNotes,
    string  Status,
    string? ResponseNotes,
    Guid?   ConsultationId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
