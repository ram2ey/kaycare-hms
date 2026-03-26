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

        // Seed all roles from CLAUDE.md
        builder.HasData(
            new Role { RoleId = 1, RoleName = "SuperAdmin",   Description = "Platform-level administrator" },
            new Role { RoleId = 2, RoleName = "Admin",        Description = "Hospital administrator" },
            new Role { RoleId = 3, RoleName = "Doctor",       Description = "Licensed physician" },
            new Role { RoleId = 4, RoleName = "Nurse",        Description = "Nursing staff" },
            new Role { RoleId = 5, RoleName = "Receptionist", Description = "Front desk / patient registration" },
            new Role { RoleId = 6, RoleName = "Pharmacist",   Description = "Pharmacy staff" }
        );
    }
}
