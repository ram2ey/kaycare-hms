namespace KayCare.Core.Entities;

public class Payment : TenantEntity
{
    public Guid     PaymentId         { get; set; }
    public Guid     BillId            { get; set; }
    public decimal  Amount            { get; set; }
    public string   PaymentMethod     { get; set; } = string.Empty;
    public string?  Reference         { get; set; }
    public Guid     ReceivedByUserId  { get; set; }
    public DateTime PaymentDate       { get; set; }
    public string?  Notes             { get; set; }

    public Bill Bill       { get; set; } = null!;
    public User ReceivedBy { get; set; } = null!;
}
