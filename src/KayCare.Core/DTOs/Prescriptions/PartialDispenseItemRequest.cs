using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Prescriptions;

public class PartialDispenseItemRequest
{
    [Required]
    public Guid PrescriptionItemId { get; set; }

    [Range(0, 9999)]
    public int QuantityToDispense { get; set; }
}
