namespace KayCare.Core.Entities;

public class NursingNote : TenantEntity
{
    public Guid    NursingNoteId  { get; set; }
    public Guid    PatientId      { get; set; }
    public Guid?   AdmissionId    { get; set; }
    public Guid    AuthorId       { get; set; }
    public string  NoteType       { get; set; } = "General";
    public string  Note           { get; set; } = string.Empty;

    public Patient   Patient    { get; set; } = null!;
    public User      Author     { get; set; } = null!;
    public Admission? Admission { get; set; }
}
