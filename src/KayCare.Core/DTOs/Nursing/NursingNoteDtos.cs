namespace KayCare.Core.DTOs.Nursing;

public record AddNursingNoteRequest(
    Guid?  AdmissionId,
    string NoteType,
    string Note
);

public record NursingNoteResponse(
    Guid     NursingNoteId,
    Guid     PatientId,
    Guid?    AdmissionId,
    string   AuthorName,
    string   NoteType,
    string   Note,
    DateTime CreatedAt
);
