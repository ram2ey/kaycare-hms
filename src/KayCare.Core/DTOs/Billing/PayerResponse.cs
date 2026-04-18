namespace KayCare.Core.DTOs.Billing;

public class PayerResponse
{
    public Guid    PayerId      { get; set; }
    public string  Name         { get; set; } = string.Empty;
    public string  Type         { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? Notes        { get; set; }
    public bool    IsActive     { get; set; }
    public DateTime CreatedAt   { get; set; }
    public DateTime UpdatedAt   { get; set; }
}
