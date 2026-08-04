using System;
using System.Threading.Tasks;
using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KayCare.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        logger.LogInformation("Applying EF Core migrations...");
        await db.Database.MigrateAsync();
        logger.LogInformation("EF Core migrations applied successfully.");

        // Check if demo tenant exists (bypassing tenant filter)
        var demoTenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.TenantCode == "demo");
        if (demoTenant is null)
        {
            logger.LogInformation("Seeding demo tenant...");
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

        var adminUser = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == "admin@demo.com");
        if (adminUser is null)
        {
            logger.LogInformation("Seeding initial admin user (admin@demo.com / Admin@1234)...");
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234", workFactor: 10);
            adminUser = new User
            {
                UserId = Guid.NewGuid(),
                TenantId = demoTenant.TenantId,
                RoleId = 2, // Admin
                Email = "admin@demo.com",
                PasswordHash = passwordHash,
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

        var patient = await db.Patients.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.MedicalRecordNumber == "KC-2026-00001");
        if (patient is null)
        {
            logger.LogInformation("Seeding sample patient...");
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

        logger.LogInformation("Demo tenant, admin user (admin@demo.com), and sample patient verification complete.");
    }
}
