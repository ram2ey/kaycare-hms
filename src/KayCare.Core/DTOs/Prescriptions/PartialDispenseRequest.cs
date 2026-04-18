using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Prescriptions;

public class PartialDispenseRequest
{
    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Required]
    public List<PartialDispenseItemRequest> Items { get; set; } = [];
}
