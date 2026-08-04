using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KayCare.Infrastructure.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(r => r.RoleId);
        builder.Property(r => r.RoleId).ValueGeneratedOnAdd();
        builder.Property(r => r.RoleName).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(200);
        builder.HasIndex(r => r.RoleName).IsUnique();

        // Seed application roles
        builder.HasData(
            new Role { RoleId = 1, RoleName = "SuperAdmin",   Description = "Platform-level administrator" },
            new Role { RoleId = 2, RoleName = "Admin",        Description = "Hospital administrator" },
            new Role { RoleId = 3, RoleName = "Doctor",       Description = "Licensed physician" },
            new Role { RoleId = 4, RoleName = "Nurse",        Description = "Nursing staff" },
            new Role { RoleId = 5, RoleName = "Receptionist", Description = "Front desk / patient registration" },
            new Role { RoleId = 6, RoleName = "Pharmacist",    Description = "Pharmacy staff" },
            new Role { RoleId = 7, RoleName = "LabTechnician",  Description = "Laboratory technician / phlebotomist" },
            new Role { RoleId = 8, RoleName = "BillingOfficer", Description = "Billing and revenue cycle staff" },
            new Role { RoleId = 9, RoleName = "PharmacyManager", Description = "Head of Pharmacy / Chief Pharmacist" },
            new Role { RoleId = 10, RoleName = "BillingManager",  Description = "Head of Billing & Revenue Management" },
            new Role { RoleId = 11, RoleName = "LabManager",      Description = "Head of Laboratory / Lab Director" },
            new Role { RoleId = 12, RoleName = "RadiologyManager", Description = "Head of Radiology & Imaging" },
            new Role { RoleId = 13, RoleName = "NurseManager",     Description = "Chief Nursing Officer / Matron" }
        );
    }
}

