using MediCloud.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCloud.Infrastructure.Data.Configurations;

public class LabTestCatalogConfiguration : IEntityTypeConfiguration<LabTestCatalog>
{
    public void Configure(EntityTypeBuilder<LabTestCatalog> builder)
    {
        builder.HasKey(t => t.LabTestCatalogId);
        builder.Property(t => t.LabTestCatalogId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(t => t.TestCode).HasMaxLength(20).IsRequired();
        builder.Property(t => t.TestName).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Department).HasMaxLength(100).IsRequired();
        builder.Property(t => t.InstrumentType).HasMaxLength(50);
        builder.Property(t => t.DefaultUnit).HasMaxLength(50);
        builder.Property(t => t.DefaultReferenceRange).HasMaxLength(100);

        builder.HasIndex(t => t.TestCode).IsUnique();

        // Seed common Ghanaian clinical lab tests
        builder.HasData(
            // ── Haematology — Beckman Coulter DxH 560 ────────────────────────
            new LabTestCatalog
            {
                LabTestCatalogId = new Guid("10000001-0000-0000-0000-000000000001"),
                TestCode = "FBC", TestName = "Full Blood Count", Department = "Haematology",
                InstrumentType = "DxH560", IsManualEntry = false, TatHours = 2,
                DefaultUnit = null, DefaultReferenceRange = null  // panel — OBX lines carry individual ranges
            },
            new LabTestCatalog
            {
                LabTestCatalogId = new Guid("10000001-0000-0000-0000-000000000002"),
                TestCode = "ESR", TestName = "Erythrocyte Sedimentation Rate", Department = "Haematology",
                InstrumentType = "DxH560", IsManualEntry = false, TatHours = 2,
                DefaultUnit = "mm/hr", DefaultReferenceRange = "0-20"
            },
            // ── Haematology — Manual ─────────────────────────────────────────
            new LabTestCatalog
            {
                LabTestCatalogId = new Guid("10000001-0000-0000-0000-000000000003"),
                TestCode = "MPS", TestName = "Blood Film for Malaria Parasite Screen", Department = "Haematology",
                InstrumentType = null, IsManualEntry = true, TatHours = 4,
                DefaultUnit = null, DefaultReferenceRange = null
            },
            // ── Chemistry — Beckman Coulter DxC 500 ──────────────────────────
            new LabTestCatalog
            {
                LabTestCatalogId = new Guid("10000001-0000-0000-0000-000000000004"),
                TestCode = "BUE", TestName = "Blood Urea and Electrolytes & Creatinine", Department = "Chemistry",
                InstrumentType = "DxC500", IsManualEntry = false, TatHours = 3,
                DefaultUnit = null, DefaultReferenceRange = null  // panel
            },
            new LabTestCatalog
            {
                LabTestCatalogId = new Guid("10000001-0000-0000-0000-000000000005"),
                TestCode = "LFT", TestName = "Liver Function Tests", Department = "Chemistry",
                InstrumentType = "DxC500", IsManualEntry = false, TatHours = 3,
                DefaultUnit = null, DefaultReferenceRange = null  // panel
            },
            new LabTestCatalog
            {
                LabTestCatalogId = new Guid("10000001-0000-0000-0000-000000000006"),
                TestCode = "MAGNESIUM", TestName = "Magnesium", Department = "Chemistry",
                InstrumentType = "DxC500", IsManualEntry = false, TatHours = 3,
                DefaultUnit = "mmol/L", DefaultReferenceRange = "0.70-1.10"
            },
            new LabTestCatalog
            {
                LabTestCatalogId = new Guid("10000001-0000-0000-0000-000000000007"),
                TestCode = "CALCIUM", TestName = "Calcium", Department = "Chemistry",
                InstrumentType = "DxC500", IsManualEntry = false, TatHours = 3,
                DefaultUnit = "mmol/L", DefaultReferenceRange = "2.10-2.55"
            },
            new LabTestCatalog
            {
                LabTestCatalogId = new Guid("10000001-0000-0000-0000-000000000008"),
                TestCode = "FBG", TestName = "Fasting Blood Glucose", Department = "Chemistry",
                InstrumentType = "DxC500", IsManualEntry = false, TatHours = 1,
                DefaultUnit = "mmol/L", DefaultReferenceRange = "3.9-5.6"
            },
            new LabTestCatalog
            {
                LabTestCatalogId = new Guid("10000001-0000-0000-0000-000000000009"),
                TestCode = "RBG", TestName = "Random Blood Glucose", Department = "Chemistry",
                InstrumentType = "DxC500", IsManualEntry = false, TatHours = 1,
                DefaultUnit = "mmol/L", DefaultReferenceRange = "3.9-7.8"
            },
            new LabTestCatalog
            {
                LabTestCatalogId = new Guid("10000001-0000-0000-0000-000000000010"),
                TestCode = "LIPID", TestName = "Lipid Profile", Department = "Chemistry",
                InstrumentType = "DxC500", IsManualEntry = false, TatHours = 3,
                DefaultUnit = null, DefaultReferenceRange = null  // panel
            },
            // ── Immunoassay — Roche cobas e411 ───────────────────────────────
            new LabTestCatalog
            {
                LabTestCatalogId = new Guid("10000001-0000-0000-0000-000000000011"),
                TestCode = "TFT", TestName = "Thyroid Function Tests (TSH, T3, T4)", Department = "Immunology",
                InstrumentType = "CobasE411", IsManualEntry = false, TatHours = 4,
                DefaultUnit = null, DefaultReferenceRange = null  // panel
            },
            new LabTestCatalog
            {
                LabTestCatalogId = new Guid("10000001-0000-0000-0000-000000000012"),
                TestCode = "VIT_B12", TestName = "Vitamin B12", Department = "Immunology",
                InstrumentType = "CobasE411", IsManualEntry = false, TatHours = 4,
                DefaultUnit = "pg/mL", DefaultReferenceRange = "197-866"
            },
            new LabTestCatalog
            {
                LabTestCatalogId = new Guid("10000001-0000-0000-0000-000000000013"),
                TestCode = "FOLATE", TestName = "Folate / Folic Acid", Department = "Immunology",
                InstrumentType = "CobasE411", IsManualEntry = false, TatHours = 4,
                DefaultUnit = "ng/mL", DefaultReferenceRange = "4.6-18.7"
            },
            new LabTestCatalog
            {
                LabTestCatalogId = new Guid("10000001-0000-0000-0000-000000000014"),
                TestCode = "HBA1C", TestName = "Glycosylated Haemoglobin (HbA1c)", Department = "Immunology",
                InstrumentType = "CobasE411", IsManualEntry = false, TatHours = 4,
                DefaultUnit = "%", DefaultReferenceRange = "4.0-5.6"
            },
            // ── Serology / Microbiology — Manual ─────────────────────────────
            new LabTestCatalog
            {
                LabTestCatalogId = new Guid("10000001-0000-0000-0000-000000000015"),
                TestCode = "TYPHOID", TestName = "Typhoid IgG / IgM", Department = "Serology",
                InstrumentType = null, IsManualEntry = true, TatHours = 6,
                DefaultUnit = null, DefaultReferenceRange = null
            },
            new LabTestCatalog
            {
                LabTestCatalogId = new Guid("10000001-0000-0000-0000-000000000016"),
                TestCode = "WIDAL", TestName = "Widal Test", Department = "Serology",
                InstrumentType = null, IsManualEntry = true, TatHours = 6,
                DefaultUnit = null, DefaultReferenceRange = null
            },
            new LabTestCatalog
            {
                LabTestCatalogId = new Guid("10000001-0000-0000-0000-000000000017"),
                TestCode = "URINE_RE", TestName = "Urine Routine Examination", Department = "Urinalysis",
                InstrumentType = null, IsManualEntry = true, TatHours = 2,
                DefaultUnit = null, DefaultReferenceRange = null
            },
            new LabTestCatalog
            {
                LabTestCatalogId = new Guid("10000001-0000-0000-0000-000000000018"),
                TestCode = "HBsAg", TestName = "Hepatitis B Surface Antigen", Department = "Serology",
                InstrumentType = null, IsManualEntry = true, TatHours = 3,
                DefaultUnit = null, DefaultReferenceRange = null
            },
            new LabTestCatalog
            {
                LabTestCatalogId = new Guid("10000001-0000-0000-0000-000000000019"),
                TestCode = "HIV", TestName = "HIV Screening Test", Department = "Serology",
                InstrumentType = null, IsManualEntry = true, TatHours = 1,
                DefaultUnit = null, DefaultReferenceRange = null
            }
        );
    }
}
