using System.ComponentModel.DataAnnotations;

namespace MediCloud.Core.DTOs.Consultations;

public class CreateConsultationRequest
{
    [Required]
    public Guid AppointmentId { get; set; }

    // Optional initial SOAP notes — can be filled incrementally via PUT
    public string? SubjectiveNotes { get; set; }
    public string? ObjectiveNotes  { get; set; }
    public string? AssessmentNotes { get; set; }
    public string? PlanNotes       { get; set; }
}
