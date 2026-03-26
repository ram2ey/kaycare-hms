namespace KayCare.Core.Entities;

public class LabOrder : TenantEntity
{
    public Guid  LabOrderId             { get; set; }
    public Guid  PatientId              { get; set; }
    public Guid? ConsultationId         { get; set; }
    public Guid? BillId                 { get; set; }
    public Guid  OrderingDoctorUserId   { get; set; }

    /// <summary>DIRECT for walk-in patients; otherwise the referring facility name.</summary>
    public string  Organisation { get; set; } = "DIRECT";
    public string  Status       { get; set; } = "Pending"; // Pending|Active|PartiallyCompleted|Completed|Signed
    public string? Notes        { get; set; }

    // Navigation
    public Patient       Patient        { get; set; } = null!;
    public Consultation? Consultation   { get; set; }
    public Bill?         Bill           { get; set; }
    public User          OrderingDoctor { get; set; } = null!;

    public ICollection<LabOrderItem> Items { get; set; } = [];
}
