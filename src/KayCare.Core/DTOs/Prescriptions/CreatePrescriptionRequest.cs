using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Prescriptions;

public class CreatePrescriptionRequest
{
    [Required]
    public Guid ConsultationId { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Required, MinLength(1)]
    public List<PrescriptionItemRequest> Items { get; set; } = [];
}
