using System.ComponentModel.DataAnnotations;

namespace MediCloud.Core.DTOs.Prescriptions;

public class DispensePrescriptionRequest
{
    /// <summary>Optional pharmacist dispensing notes (appended to existing notes).</summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }
}
