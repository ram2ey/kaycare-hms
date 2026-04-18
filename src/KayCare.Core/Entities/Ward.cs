namespace KayCare.Core.Entities;

public class Ward : TenantEntity
{
    public Guid    WardId      { get; set; }
    public string  Name        { get; set; } = string.Empty;
    public string  WardType    { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DailyRate   { get; set; }
    public bool    IsActive    { get; set; } = true;

    public ICollection<Bed> Beds { get; set; } = [];
}
