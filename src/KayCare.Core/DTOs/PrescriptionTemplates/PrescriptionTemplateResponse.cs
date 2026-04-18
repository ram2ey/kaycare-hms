namespace KayCare.Core.DTOs.PrescriptionTemplates;

public class PrescriptionTemplateResponse
{
    public Guid      TemplateId      { get; set; }
    public string    Name            { get; set; } = string.Empty;
    public string?   Description     { get; set; }
    public bool      IsShared        { get; set; }
    public string    CreatedByName   { get; set; } = string.Empty;
    public int       ItemCount       { get; set; }
    public DateTime  CreatedAt       { get; set; }
}
