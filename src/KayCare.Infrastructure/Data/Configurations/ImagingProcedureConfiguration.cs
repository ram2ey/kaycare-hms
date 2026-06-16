using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace KayCare.Infrastructure.Data.Configurations;

public class ImagingProcedureConfiguration : IEntityTypeConfiguration<ImagingProcedure>
{
    public void Configure(EntityTypeBuilder<ImagingProcedure> builder)
    {
        builder.HasKey(p => p.ImagingProcedureId);
        builder.Property(p => p.ImagingProcedureId).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(p => p.ProcedureCode).HasMaxLength(20).IsRequired();
        builder.Property(p => p.ProcedureName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Modality).HasMaxLength(20).IsRequired();
        builder.Property(p => p.BodyPart).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Department).HasMaxLength(100).IsRequired();
        builder.HasIndex(p => p.ProcedureCode).IsUnique();

        builder.HasData(
            new ImagingProcedure { ImagingProcedureId = new Guid("20000001-0000-0000-0000-000000000001"), ProcedureCode = "XR-CHEST-PA",  ProcedureName = "X-Ray Chest PA",           Modality = "XR",  BodyPart = "Chest",         TatHours = 2 },
            new ImagingProcedure { ImagingProcedureId = new Guid("20000001-0000-0000-0000-000000000002"), ProcedureCode = "XR-ABDOMEN",   ProcedureName = "X-Ray Abdomen",             Modality = "XR",  BodyPart = "Abdomen",        TatHours = 2 },
            new ImagingProcedure { ImagingProcedureId = new Guid("20000001-0000-0000-0000-000000000003"), ProcedureCode = "US-ABDOMEN",   ProcedureName = "Ultrasound Abdomen",         Modality = "US",  BodyPart = "Abdomen",        TatHours = 4 },
            new ImagingProcedure { ImagingProcedureId = new Guid("20000001-0000-0000-0000-000000000004"), ProcedureCode = "US-PELVIS",    ProcedureName = "Ultrasound Pelvis",          Modality = "US",  BodyPart = "Pelvis",         TatHours = 4 },
            new ImagingProcedure { ImagingProcedureId = new Guid("20000001-0000-0000-0000-000000000005"), ProcedureCode = "CT-HEAD",      ProcedureName = "CT Head",                   Modality = "CT",  BodyPart = "Head",           TatHours = 6 },
            new ImagingProcedure { ImagingProcedureId = new Guid("20000001-0000-0000-0000-000000000006"), ProcedureCode = "CT-CHEST",     ProcedureName = "CT Chest",                  Modality = "CT",  BodyPart = "Chest",          TatHours = 6 },
            new ImagingProcedure { ImagingProcedureId = new Guid("20000001-0000-0000-0000-000000000007"), ProcedureCode = "CT-ABDO-PELV", ProcedureName = "CT Abdomen & Pelvis",       Modality = "CT",  BodyPart = "Abdomen/Pelvis", TatHours = 8 },
            new ImagingProcedure { ImagingProcedureId = new Guid("20000001-0000-0000-0000-000000000008"), ProcedureCode = "MRI-BRAIN",    ProcedureName = "MRI Brain",                 Modality = "MRI", BodyPart = "Brain",          TatHours = 12 },
            new ImagingProcedure { ImagingProcedureId = new Guid("20000001-0000-0000-0000-000000000009"), ProcedureCode = "MRI-SPINE",    ProcedureName = "MRI Spine",                 Modality = "MRI", BodyPart = "Spine",          TatHours = 12 },
            new ImagingProcedure { ImagingProcedureId = new Guid("20000001-0000-0000-0000-000000000010"), ProcedureCode = "MAMMO-BI",     ProcedureName = "Mammography Bilateral",     Modality = "MG",  BodyPart = "Breast",         TatHours = 8 }
        );
    }
}
