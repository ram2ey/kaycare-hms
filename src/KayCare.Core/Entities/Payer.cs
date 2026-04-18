namespace KayCare.Core.Entities;

public class Payer : TenantEntity
{
    public Guid    PayerId      { get; set; }
    public string  Name         { get; set; } = string.Empty;
    public string  Type         { get; set; } = string.Empty; // PayerType constants
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? Notes        { get; set; }
    public bool    IsActive     { get; set; } = true;
}
