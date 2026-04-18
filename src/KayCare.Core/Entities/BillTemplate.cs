namespace KayCare.Core.Entities;

public class BillTemplate : TenantEntity
{
    public Guid    BillTemplateId { get; set; }
    public string  Name           { get; set; } = string.Empty;
    public string? Description    { get; set; }
    public string? Category       { get; set; }
    public bool    IsActive       { get; set; } = true;

    public ICollection<BillTemplateItem> Items { get; set; } = [];
}
