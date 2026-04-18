namespace KayCare.Core.DTOs.Inpatient;

public class WardResponse
{
    public Guid     WardId        { get; set; }
    public string   Name          { get; set; } = string.Empty;
    public string   WardType      { get; set; } = string.Empty;
    public string?  Description   { get; set; }
    public decimal  DailyRate     { get; set; }
    public bool     IsActive      { get; set; }
    public int      TotalBeds     { get; set; }
    public int      AvailableBeds { get; set; }
    public int      OccupiedBeds  { get; set; }
    public int      MaintenanceBeds { get; set; }
    public DateTime CreatedAt     { get; set; }
    public DateTime UpdatedAt     { get; set; }
}

public class SaveWardRequest
{
    public string  Name        { get; set; } = string.Empty;
    public string  WardType    { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DailyRate   { get; set; }
    public bool    IsActive    { get; set; } = true;
}

public class BedResponse
{
    public Guid     BedId     { get; set; }
    public Guid     WardId    { get; set; }
    public string   WardName  { get; set; } = string.Empty;
    public string   BedNumber { get; set; } = string.Empty;
    public string   Status    { get; set; } = string.Empty;
    public string?  Notes     { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SaveBedRequest
{
    public string  BedNumber { get; set; } = string.Empty;
    public string? Notes     { get; set; }
}

public class UpdateBedStatusRequest
{
    public string  Status { get; set; } = string.Empty;
    public string? Notes  { get; set; }
}
