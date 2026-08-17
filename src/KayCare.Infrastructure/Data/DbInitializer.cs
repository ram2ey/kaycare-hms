using System;
using System.Threading.Tasks;
using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KayCare.Infrastructure.Data;

public static class DbInitializer
{
    private const string MigrationLockName = "KayCareMigrations";

    /// <summary>
    /// Applies pending EF Core migrations. Always safe to run, in every environment. Wrapped in a
    /// session-level Postgres advisory lock so only one instance actually runs migrations if the
    /// app is ever scaled to 2+ instances that all start up at once — the others block here until
    /// the first finishes, then find nothing pending.
    /// </summary>
    public static async Task MigrateAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.AcquireSessionAdvisoryLockAsync(MigrationLockName, default);
        try
        {
            logger.LogInformation("Applying EF Core migrations...");
            await db.Database.MigrateAsync();
            logger.LogInformation("EF Core migrations applied successfully.");
        }
        finally
        {
            await db.ReleaseSessionAdvisoryLockAsync(MigrationLockName, default);
        }
    }

    /// <summary>
    /// Seeds the "demo" tenant and its sample accounts/data. Intended for local development and
    /// deliberately opted-in demo/sales instances only — must never run unconditionally in a real
    /// Production deployment, since it re-activates the demo tenant/accounts if they were disabled.
    /// Credentials/lockout state are only ever set when an account is first created, never reset
    /// on a subsequent run, so an operator who has since disabled or rotated a demo account is not
    /// silently overridden.
    /// </summary>
    public static async Task SeedDemoDataAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Ensure demo tenant exists
        var demoTenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.TenantCode == "demo");
        if (demoTenant is null)
        {
            logger.LogInformation("Creating demo tenant...");
            demoTenant = new Tenant
            {
                TenantId = Guid.NewGuid(),
                TenantCode = "demo",
                TenantName = "Demo Hospital",
                Subdomain = "demo",
                SubscriptionPlan = "Standard",
                IsActive = true,
                MaxUsers = 100,
                StorageQuotaGB = 50,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Tenants.Add(demoTenant);
            await db.SaveChangesAsync();
        }

        // 2. Ensure Admin User (admin@demo.com / Admin@1234)
        var adminUser = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.TenantId == demoTenant.TenantId && u.Email == "admin@demo.com");
        if (adminUser is null)
        {
            logger.LogInformation("Seeding Admin user (admin@demo.com)...");
            adminUser = new User
            {
                UserId = Guid.NewGuid(),
                TenantId = demoTenant.TenantId,
                RoleId = 2, // Admin
                Email = "admin@demo.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234", workFactor: 10),
                FirstName = "Admin",
                LastName = "User",
                IsActive = true,
                MustChangePassword = false,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Users.Add(adminUser);
            await db.SaveChangesAsync();
        }

        // 3. Ensure Doctor User (doctor@demo.com / Doctor@1234)
        var doctorUser = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.TenantId == demoTenant.TenantId && u.Email == "doctor@demo.com");
        if (doctorUser is null)
        {
            logger.LogInformation("Seeding Doctor user (doctor@demo.com)...");
            doctorUser = new User
            {
                UserId = Guid.NewGuid(),
                TenantId = demoTenant.TenantId,
                RoleId = 3, // Doctor
                Email = "doctor@demo.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Doctor@1234", workFactor: 10),
                FirstName = "Kwaku",
                LastName = "Appiah",
                LicenseNumber = "MDC/REG/2026/892",
                Department = "General Medicine",
                IsActive = true,
                MustChangePassword = false,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Users.Add(doctorUser);
            await db.SaveChangesAsync();
        }

        // 4. Ensure Nurse User (nurse@demo.com / Nurse@1234)
        var nurseUser = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.TenantId == demoTenant.TenantId && u.Email == "nurse@demo.com");
        if (nurseUser is null)
        {
            logger.LogInformation("Seeding Nurse user (nurse@demo.com)...");
            nurseUser = new User
            {
                UserId = Guid.NewGuid(),
                TenantId = demoTenant.TenantId,
                RoleId = 4, // Nurse
                Email = "nurse@demo.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Nurse@1234", workFactor: 10),
                FirstName = "Ama",
                LastName = "Osei",
                LicenseNumber = "NMC/REG/2026/410",
                Department = "Outpatient Nursing",
                IsActive = true,
                MustChangePassword = false,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Users.Add(nurseUser);
            await db.SaveChangesAsync();
        }

        // 5. Ensure Sample Patient (KC-2026-00001)
        var patient = await db.Patients.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.TenantId == demoTenant.TenantId && p.MedicalRecordNumber == "KC-2026-00001");
        if (patient is null)
        {
            logger.LogInformation("Seeding sample patient (Kwame Mensah)...");
            patient = new Patient
            {
                PatientId = Guid.NewGuid(),
                TenantId = demoTenant.TenantId,
                MedicalRecordNumber = "KC-2026-00001",
                FirstName = "Kwame",
                LastName = "Mensah",
                DateOfBirth = new DateOnly(1988, 5, 12),
                Gender = "Male",
                PhoneNumber = "+233241234567",
                Email = "kwame.mensah@example.com",
                AddressLine1 = "15 Independence Avenue, Accra",
                EmergencyContactName = "Abena Mensah",
                EmergencyContactPhone = "+233249876543",
                BloodType = "O+",
                IsActive = true,
                RegisteredByUserId = adminUser.UserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Patients.Add(patient);
            await db.SaveChangesAsync();
        }

        logger.LogInformation("Demo accounts (admin@demo.com, doctor@demo.com, nurse@demo.com) successfully verified and seeded.");

        // 6. Seed extended demo dataset (Patients, Consultations, Wards, Labs, Inventory, Billing, Claims)
        await DemoDataSeeder.SeedAllAsync(db, logger);
    }
}
