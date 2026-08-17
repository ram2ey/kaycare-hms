using KayCare.Core.Entities;
using KayCare.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant>           Tenants           => Set<Tenant>();
    public DbSet<FacilitySettings> FacilitySettings  => Set<FacilitySettings>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientAllergy> PatientAllergies => Set<PatientAllergy>();
    public DbSet<Appointment>      Appointments      => Set<Appointment>();
    public DbSet<Consultation>     Consultations     => Set<Consultation>();
    public DbSet<Prescription>             Prescriptions             => Set<Prescription>();
    public DbSet<PrescriptionItem>         PrescriptionItems         => Set<PrescriptionItem>();
    public DbSet<PrescriptionTemplate>     PrescriptionTemplates     => Set<PrescriptionTemplate>();
    public DbSet<PrescriptionTemplateItem> PrescriptionTemplateItems => Set<PrescriptionTemplateItem>();
    public DbSet<DispenseEvent>            DispenseEvents            => Set<DispenseEvent>();
    public DbSet<DispenseEventItem>        DispenseEventItems        => Set<DispenseEventItem>();
    public DbSet<ServiceCatalogItem>  ServiceCatalogItems  => Set<ServiceCatalogItem>();
    public DbSet<BillTemplate>        BillTemplates        => Set<BillTemplate>();
    public DbSet<BillTemplateItem>    BillTemplateItems    => Set<BillTemplateItem>();
    public DbSet<Payer>            Payers            => Set<Payer>();
    public DbSet<Bill>             Bills             => Set<Bill>();
    public DbSet<BillItem>         BillItems         => Set<BillItem>();
    public DbSet<BillAdjustment>   BillAdjustments   => Set<BillAdjustment>();
    public DbSet<Payment>          Payments          => Set<Payment>();
    public DbSet<PatientDocument>  PatientDocuments  => Set<PatientDocument>();
    public DbSet<LabResult>        LabResults        => Set<LabResult>();
    public DbSet<LabObservation>   LabObservations   => Set<LabObservation>();
    public DbSet<LabTestCatalog>   LabTestCatalog    => Set<LabTestCatalog>();
    public DbSet<LabOrder>         LabOrders         => Set<LabOrder>();
    public DbSet<LabOrderItem>     LabOrderItems     => Set<LabOrderItem>();
    public DbSet<InsuranceClaim>    InsuranceClaims   => Set<InsuranceClaim>();
    public DbSet<CreditNote>        CreditNotes       => Set<CreditNote>();
    public DbSet<Refund>            Refunds           => Set<Refund>();
    public DbSet<DrugInventory>    DrugInventory     => Set<DrugInventory>();
    public DbSet<StockMovement>    StockMovements    => Set<StockMovement>();
    public DbSet<Supplier>         Suppliers         => Set<Supplier>();
    public DbSet<PurchaseOrder>    PurchaseOrders    => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<Ward>              Wards              => Set<Ward>();
    public DbSet<Bed>               Beds               => Set<Bed>();
    public DbSet<Admission>         Admissions         => Set<Admission>();
    public DbSet<AdmissionTransfer> AdmissionTransfers => Set<AdmissionTransfer>();
    public DbSet<InpatientCharge>          InpatientCharges          => Set<InpatientCharge>();
    public DbSet<VitalSigns>               VitalSigns                => Set<VitalSigns>();
    public DbSet<NursingNote>              NursingNotes              => Set<NursingNote>();
    public DbSet<MedicationAdministration> MedicationAdministrations => Set<MedicationAdministration>();
    public DbSet<Referral>                 Referrals                 => Set<Referral>();
    public DbSet<IcdCode>                  IcdCodes                  => Set<IcdCode>();
    public DbSet<AuditLog>                 AuditLogs                 => Set<AuditLog>();
    public DbSet<ImagingProcedure>        ImagingProcedures         => Set<ImagingProcedure>();
    public DbSet<RadiologyOrder>          RadiologyOrders           => Set<RadiologyOrder>();
    public DbSet<RadiologyOrderItem>      RadiologyOrderItems       => Set<RadiologyOrderItem>();
    public DbSet<CriticalCallLog>         CriticalCallLogs          => Set<CriticalCallLog>();
    public DbSet<PayerTariff>             PayerTariffs              => Set<PayerTariff>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global tenant isolation — every tenant-scoped entity gets this filter
        modelBuilder.Entity<User>()
            .HasQueryFilter(u => u.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Patient>()
            .HasQueryFilter(p => p.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<PatientAllergy>()
            .HasQueryFilter(a => a.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Appointment>()
            .HasQueryFilter(a => a.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Consultation>()
            .HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Prescription>()
            .HasQueryFilter(p => p.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<PrescriptionItem>()
            .HasQueryFilter(i => i.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<ServiceCatalogItem>()
            .HasQueryFilter(s => s.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Payer>()
            .HasQueryFilter(p => p.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Bill>()
            .HasQueryFilter(b => b.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<BillItem>()
            .HasQueryFilter(i => i.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<BillAdjustment>()
            .HasQueryFilter(a => a.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Payment>()
            .HasQueryFilter(p => p.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<PatientDocument>()
            .HasQueryFilter(d => d.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<LabResult>()
            .HasQueryFilter(r => r.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<LabObservation>()
            .HasQueryFilter(o => o.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<LabOrder>()
            .HasQueryFilter(o => o.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<LabOrderItem>()
            .HasQueryFilter(i => i.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<AuditLog>()
            .HasQueryFilter(a => a.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<PrescriptionTemplate>()
            .HasQueryFilter(t => t.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<PrescriptionTemplateItem>()
            .HasQueryFilter(i => i.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<DispenseEvent>()
            .HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<DispenseEventItem>()
            .HasQueryFilter(i => i.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<InsuranceClaim>()
            .HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<CreditNote>()
            .HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Refund>()
            .HasQueryFilter(r => r.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<FacilitySettings>()
            .HasQueryFilter(f => f.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<BillTemplate>()
            .HasQueryFilter(t => t.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<BillTemplateItem>()
            .HasQueryFilter(i => i.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<DrugInventory>()
            .HasQueryFilter(d => d.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<StockMovement>()
            .HasQueryFilter(m => m.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Supplier>()
            .HasQueryFilter(s => s.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<PurchaseOrder>()
            .HasQueryFilter(po => po.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<PurchaseOrderItem>()
            .HasQueryFilter(i => i.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Ward>()
            .HasQueryFilter(w => w.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Bed>()
            .HasQueryFilter(b => b.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Admission>()
            .HasQueryFilter(a => a.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<AdmissionTransfer>()
            .HasQueryFilter(t => t.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<InpatientCharge>()
            .HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<VitalSigns>()
            .HasQueryFilter(v => v.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<NursingNote>()
            .HasQueryFilter(n => n.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<MedicationAdministration>()
            .HasQueryFilter(m => m.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Referral>()
            .HasQueryFilter(r => r.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<RadiologyOrder>()
            .HasQueryFilter(r => r.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<RadiologyOrderItem>()
            .HasQueryFilter(i => i.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<CriticalCallLog>()
            .HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<PayerTariff>()
            .HasQueryFilter(t => t.TenantId == _tenantContext.TenantId);

        // SQL Server → PostgreSQL translation loop. This app runs on Npgsql/Postgres only (see
        // UseNpgsql in DependencyInjection.cs) — there is no SQL Server target — but every
        // *Configuration.cs file still writes SQL Server-syntax default/computed-column SQL
        // (HasDefaultValueSql("NEWSEQUENTIALID()"), bracket-quoted computed columns) rather than
        // Postgres syntax directly, a holdover from before this app moved off SQL Server. Still
        // load-bearing: without this loop, every entity using NEWSEQUENTIALID()/SYSUTCDATETIME()
        // as its ID/timestamp default would fail against Postgres, which has neither function.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var defaultSql = property.GetDefaultValueSql();
                if (defaultSql != null)
                {
                    if (defaultSql.Contains("NEWSEQUENTIALID()", StringComparison.OrdinalIgnoreCase))
                    {
                        property.SetDefaultValueSql("gen_random_uuid()");
                    }
                    else if (defaultSql.Contains("SYSUTCDATETIME()", StringComparison.OrdinalIgnoreCase))
                    {
                        property.SetDefaultValueSql("CURRENT_TIMESTAMP");
                    }
                }

                var computedSql = property.GetComputedColumnSql();
                if (computedSql != null)
                {
                    var newComputedSql = computedSql.Replace("[", "\"").Replace("]", "\"");
                    property.SetComputedColumnSql(newComputedSql);
                }

                var columnType = property.GetColumnType();
                if (columnType != null && columnType.Contains("nvarchar(max)", StringComparison.OrdinalIgnoreCase))
                {
                    property.SetColumnType("text");
                }
            }
        }

        // Enforce UTC DateTime kind for PostgreSQL compatibility
        var dateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableDateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<TenantEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.TenantId == Guid.Empty)
                    {
                        entry.Entity.TenantId = _tenantContext.TenantId;
                    }
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
