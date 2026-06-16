using System;
using System.Collections.Generic;

namespace KayCare.Core.Entities;

public class RadiologyOrder : TenantEntity
{
    public Guid  RadiologyOrderId      { get; set; }
    public Guid  PatientId             { get; set; }
    public Guid? BillId                { get; set; }
    public Guid  OrderingDoctorUserId  { get; set; }

    public string  Priority           { get; set; } = "Routine";
    public string  Status             { get; set; } = "Pending";
    public string? ClinicalIndication { get; set; }
    public string? Notes              { get; set; }

    public Patient Patient        { get; set; } = null!;
    public Bill?   Bill           { get; set; }
    public User    OrderingDoctor { get; set; } = null!;

    public ICollection<RadiologyOrderItem> Items { get; set; } = [];
}
