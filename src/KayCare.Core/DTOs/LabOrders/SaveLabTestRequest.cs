using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.LabOrders;

public class SaveLabTestRequest
{
    [Required, MaxLength(20)]
    public string TestCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string TestName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Department { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? InstrumentType { get; set; }

    public bool IsManualEntry { get; set; }

    [Range(1, 168)]
    public int TatHours { get; set; } = 4;

    [MaxLength(50)]
    public string? DefaultUnit { get; set; }

    [MaxLength(100)]
    public string? DefaultReferenceRange { get; set; }

    [MaxLength(100)]
    public string? CriticalReferenceRange { get; set; }

    public bool IsActive { get; set; } = true;
}
