namespace KayCare.Core.Entities;

public class PrescriptionTemplate : TenantEntity
{
    public Guid   TemplateId        { get; set; }
    public Guid   CreatedByUserId   { get; set; }
    public string Name              { get; set; } = string.Empty;
    public string? Description      { get; set; }
    public bool   IsShared          { get; set; }

    public User CreatedBy { get; set; } = null!;
    public ICollection<PrescriptionTemplateItem> Items { get; set; } = [];
}
