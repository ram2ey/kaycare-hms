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

    public DbSet<Tenant> Tenants => Set<Tenant>();
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
    public DbSet<ServiceCatalogItem> ServiceCatalogItems => Set<ServiceCatalogItem>();
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
    public DbSet<AuditLog>         AuditLogs         => Set<AuditLog>();

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
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<TenantEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.TenantId = _tenantContext.TenantId;
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
