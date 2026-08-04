using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KayCare.Core.Constants;
using KayCare.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KayCare.Infrastructure.Data;

public static class DemoDataSeeder
{
    public static async Task SeedAllAsync(AppDbContext db, ILogger logger)
    {
        var demoTenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.TenantCode == "demo");
        if (demoTenant is null)
        {
            logger.LogWarning("Demo tenant not found. Skipping extended demo data seeding.");
            return;
        }

        var tenantId = demoTenant.TenantId;

        // Idempotency check: if patients >= 5, demo data is already seeded
        var existingPatientCount = await db.Patients.IgnoreQueryFilters().CountAsync(p => p.TenantId == tenantId);
        if (existingPatientCount >= 5)
        {
            logger.LogInformation("Demo data is already populated ({Count} patients found). Skipping.", existingPatientCount);
            return;
        }

        logger.LogInformation("Seeding comprehensive demo data for 'Demo Hospital' (Tenant ID: {TenantId})...", tenantId);

        var now = DateTime.UtcNow;

        // ─────────────────────────────────────────────────────────────────
        // 1. FACILITY SETTINGS
        // ─────────────────────────────────────────────────────────────────
        var settings = await db.FacilitySettings.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.TenantId == tenantId);
        if (settings is null)
        {
            settings = new FacilitySettings
            {
                FacilitySettingsId = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityName = "Demo Hospital - Accra Central",
                Address = "15 Independence Avenue, Ridge, Accra",
                Phone = "+233 30 212 3456",
                Email = "info@demohospital.com.gh",
                CreatedAt = now,
                UpdatedAt = now
            };
            db.FacilitySettings.Add(settings);
            await db.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────────────
        // 2. USERS / STAFF ACCOUNTS
        // ─────────────────────────────────────────────────────────────────
        var adminUser = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == "admin@demo.com");
        var doctorUser1 = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == "doctor@demo.com"); // Dr. Kwaku Appiah
        var nurseUser1 = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == "nurse@demo.com");  // Ama Osei

        var defaultHash = BCrypt.Net.BCrypt.HashPassword("Demo@1234", workFactor: 10);

        var doctorUser2 = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == "dr.mensah@demo.com");
        if (doctorUser2 is null)
        {
            doctorUser2 = new User
            {
                UserId = Guid.NewGuid(), TenantId = tenantId, RoleId = 3, Email = "dr.mensah@demo.com",
                PasswordHash = defaultHash, FirstName = "Efua", LastName = "Mensah", LicenseNumber = "MDC/REG/2026/104",
                Department = "Pediatrics", IsActive = true, MustChangePassword = false, CreatedAt = now, UpdatedAt = now
            };
            db.Users.Add(doctorUser2);
        }

        var doctorUser3 = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == "dr.owusu@demo.com");
        if (doctorUser3 is null)
        {
            doctorUser3 = new User
            {
                UserId = Guid.NewGuid(), TenantId = tenantId, RoleId = 3, Email = "dr.owusu@demo.com",
                PasswordHash = defaultHash, FirstName = "Kojo", LastName = "Owusu", LicenseNumber = "MDC/REG/2026/552",
                Department = "General Surgery", IsActive = true, MustChangePassword = false, CreatedAt = now, UpdatedAt = now
            };
            db.Users.Add(doctorUser3);
        }

        var nurseUser2 = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == "nurse.abena@demo.com");
        if (nurseUser2 is null)
        {
            nurseUser2 = new User
            {
                UserId = Guid.NewGuid(), TenantId = tenantId, RoleId = 4, Email = "nurse.abena@demo.com",
                PasswordHash = defaultHash, FirstName = "Abena", LastName = "Kyei", LicenseNumber = "NMC/REG/2026/780",
                Department = "Inpatient Nursing", IsActive = true, MustChangePassword = false, CreatedAt = now, UpdatedAt = now
            };
            db.Users.Add(nurseUser2);
        }

        var receptionist = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == "receptionist@demo.com");
        if (receptionist is null)
        {
            receptionist = new User
            {
                UserId = Guid.NewGuid(), TenantId = tenantId, RoleId = 5, Email = "receptionist@demo.com",
                PasswordHash = defaultHash, FirstName = "Esi", LastName = "Mansa", LicenseNumber = "REC/2026/012",
                Department = "Front Desk & Admissions", IsActive = true, MustChangePassword = false, CreatedAt = now, UpdatedAt = now
            };
            db.Users.Add(receptionist);
        }

        var pharmacist = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == "pharmacist@demo.com");
        if (pharmacist is null)
        {
            pharmacist = new User
            {
                UserId = Guid.NewGuid(), TenantId = tenantId, RoleId = 6, Email = "pharmacist@demo.com",
                PasswordHash = defaultHash, FirstName = "Kojo", LastName = "Antwi", LicenseNumber = "PSGH/REG/2026/303",
                Department = "Main Pharmacy", IsActive = true, MustChangePassword = false, CreatedAt = now, UpdatedAt = now
            };
            db.Users.Add(pharmacist);
        }

        var labTech = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == "labtech@demo.com");
        if (labTech is null)
        {
            labTech = new User
            {
                UserId = Guid.NewGuid(), TenantId = tenantId, RoleId = 7, Email = "labtech@demo.com",
                PasswordHash = defaultHash, FirstName = "Yaw", LastName = "Dankwa", LicenseNumber = "AHS/REG/2026/419",
                Department = "Clinical Diagnostics Lab", IsActive = true, MustChangePassword = false, CreatedAt = now, UpdatedAt = now
            };
            db.Users.Add(labTech);
        }

        var billingOfficer = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == "billing@demo.com");
        if (billingOfficer is null)
        {
            billingOfficer = new User
            {
                UserId = Guid.NewGuid(), TenantId = tenantId, RoleId = 8, Email = "billing@demo.com",
                PasswordHash = defaultHash, FirstName = "Akosua", LastName = "Serwaa", LicenseNumber = "ACC/2026/088",
                Department = "Accounts & Revenue Cycle", IsActive = true, MustChangePassword = false, CreatedAt = now, UpdatedAt = now
            };
            db.Users.Add(billingOfficer);
        }

        await db.SaveChangesAsync();

        // ─────────────────────────────────────────────────────────────────
        // 3. PAYERS
        // ─────────────────────────────────────────────────────────────────
        var nhisPayer = await db.Payers.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Name == "National Health Insurance Scheme (NHIS)");
        if (nhisPayer is null)
        {
            nhisPayer = new Payer
            {
                PayerId = Guid.NewGuid(), TenantId = tenantId, Name = "National Health Insurance Scheme (NHIS)",
                Type = PayerType.NHIS, ContactPhone = "+233 30 221 6800", ContactEmail = "claims@nhis.gov.gh",
                Notes = "Ghana National Health Insurance Scheme public provider", IsActive = true, CreatedAt = now, UpdatedAt = now
            };
            db.Payers.Add(nhisPayer);
        }

        var glicoPayer = await db.Payers.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Name == "Glico Healthcare");
        if (glicoPayer is null)
        {
            glicoPayer = new Payer
            {
                PayerId = Guid.NewGuid(), TenantId = tenantId, Name = "Glico Healthcare",
                Type = PayerType.PrivateInsurance, ContactPhone = "+233 30 224 6852", ContactEmail = "info@glicohealth.com",
                Notes = "Private health insurance corporate partner", IsActive = true, CreatedAt = now, UpdatedAt = now
            };
            db.Payers.Add(glicoPayer);
        }

        var selfPayer = await db.Payers.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Name == "Self-Pay (Cash)");
        if (selfPayer is null)
        {
            selfPayer = new Payer
            {
                PayerId = Guid.NewGuid(), TenantId = tenantId, Name = "Self-Pay (Cash)",
                Type = "SelfPay", ContactPhone = null, ContactEmail = null,
                Notes = "Out-of-pocket cash payments", IsActive = true, CreatedAt = now, UpdatedAt = now
            };
            db.Payers.Add(selfPayer);
        }

        await db.SaveChangesAsync();

        // ─────────────────────────────────────────────────────────────────
        // 4. SERVICE CATALOG ITEMS
        // ─────────────────────────────────────────────────────────────────
        var catalogItems = new List<ServiceCatalogItem>
        {
            new() { ServiceCatalogItemId = Guid.NewGuid(), TenantId = tenantId, Name = "General OPD Consultation", Category = "Consultation", UnitPrice = 80.00m, Description = "Standard Outpatient Doctor Consultation", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { ServiceCatalogItemId = Guid.NewGuid(), TenantId = tenantId, Name = "Specialist Consultation", Category = "Consultation", UnitPrice = 200.00m, Description = "Specialist Physician / Surgeon Review", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { ServiceCatalogItemId = Guid.NewGuid(), TenantId = tenantId, Name = "Emergency Triage & Assessment", Category = "Emergency", UnitPrice = 150.00m, Description = "Acute Emergency Triage and Stabilization", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { ServiceCatalogItemId = Guid.NewGuid(), TenantId = tenantId, Name = "Daily Inpatient Accommodation (General Ward)", Category = "Inpatient", UnitPrice = 120.00m, Description = "Standard Inpatient Ward Bed rate per day", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { ServiceCatalogItemId = Guid.NewGuid(), TenantId = tenantId, Name = "Daily Inpatient Accommodation (Surgical Ward)", Category = "Inpatient", UnitPrice = 150.00m, Description = "Surgical Inpatient Ward Bed rate per day", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { ServiceCatalogItemId = Guid.NewGuid(), TenantId = tenantId, Name = "Nursing Care Daily Fee", Category = "Nursing", UnitPrice = 50.00m, Description = "24-Hour Nursing Monitoring and Care", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { ServiceCatalogItemId = Guid.NewGuid(), TenantId = tenantId, Name = "Chest X-Ray PA View", Category = "Radiology", UnitPrice = 180.00m, Description = "Posterior-Anterior Digital Chest X-Ray", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { ServiceCatalogItemId = Guid.NewGuid(), TenantId = tenantId, Name = "Abdominopelvic Ultrasound", Category = "Radiology", UnitPrice = 250.00m, Description = "Complete Abdomen and Pelvis Sonogram", IsActive = true, CreatedAt = now, UpdatedAt = now }
        };

        foreach (var ci in catalogItems)
        {
            var exists = await db.ServiceCatalogItems.IgnoreQueryFilters().AnyAsync(s => s.TenantId == tenantId && s.Name == ci.Name);
            if (!exists) db.ServiceCatalogItems.Add(ci);
        }
        await db.SaveChangesAsync();

        // ─────────────────────────────────────────────────────────────────
        // 5. PATIENTS & ALLERGIES
        // ─────────────────────────────────────────────────────────────────
        var patientsToSeed = new List<Patient>
        {
            new()
            {
                PatientId = Guid.NewGuid(), TenantId = tenantId, MedicalRecordNumber = "KC-2026-00002",
                FirstName = "Abena", LastName = "Osei-Bonsu", DateOfBirth = new DateOnly(1997, 3, 24), Gender = "Female",
                BloodType = "A+", Email = "abena.osei@example.com", PhoneNumber = "+233244112233", Country = "GH",
                AddressLine1 = "24 Cantonments Road, Accra", EmergencyContactName = "Kofi Osei-Bonsu", EmergencyContactPhone = "+233208112233",
                NhisNumber = "GH-NHIS-8821940", InsuranceProvider = "National Health Insurance Scheme (NHIS)", InsurancePolicyNumber = "NHIS-8821940-01",
                HasAllergies = true, HasChronicConditions = false, IsActive = true, RegisteredByUserId = adminUser!.UserId, CreatedAt = now.AddDays(-28), UpdatedAt = now
            },
            new()
            {
                PatientId = Guid.NewGuid(), TenantId = tenantId, MedicalRecordNumber = "KC-2026-00003",
                FirstName = "Kofi", LastName = "Adjei", DateOfBirth = new DateOnly(1974, 9, 15), Gender = "Male",
                BloodType = "O-", Email = "kofi.adjei@example.com", PhoneNumber = "+233277998877", Country = "GH",
                AddressLine1 = "12 Ring Road Central, Accra", EmergencyContactName = "Yaa Adjei", EmergencyContactPhone = "+233243998877",
                InsuranceProvider = "Glico Healthcare", InsurancePolicyNumber = "GLC-994012", InsuranceGroupNumber = "CORP-ACCRA",
                HasAllergies = false, HasChronicConditions = true, IsActive = true, RegisteredByUserId = adminUser.UserId, CreatedAt = now.AddDays(-25), UpdatedAt = now
            },
            new()
            {
                PatientId = Guid.NewGuid(), TenantId = tenantId, MedicalRecordNumber = "KC-2026-00004",
                FirstName = "Yaw", LastName = "Boateng", DateOfBirth = new DateOnly(1959, 11, 2), Gender = "Male",
                BloodType = "B+", Email = "yaw.boateng@example.com", PhoneNumber = "+233501239876", Country = "GH",
                AddressLine1 = "Plot 8 Block B, East Legon, Accra", EmergencyContactName = "Kwaku Boateng", EmergencyContactPhone = "+233244223344",
                NhisNumber = "GH-NHIS-1192045", InsuranceProvider = "National Health Insurance Scheme (NHIS)", InsurancePolicyNumber = "NHIS-1192045-02",
                HasAllergies = true, HasChronicConditions = true, IsActive = true, RegisteredByUserId = adminUser.UserId, CreatedAt = now.AddDays(-20), UpdatedAt = now
            },
            new()
            {
                PatientId = Guid.NewGuid(), TenantId = tenantId, MedicalRecordNumber = "KC-2026-00005",
                FirstName = "Akua", LastName = "Donkor", DateOfBirth = new DateOnly(2022, 6, 18), Gender = "Female",
                BloodType = "O+", Email = null, PhoneNumber = "+233245667788", Country = "GH",
                AddressLine1 = "House 14, Dansoman, Accra", EmergencyContactName = "Ama Donkor (Mother)", EmergencyContactPhone = "+233245667788",
                HasAllergies = false, HasChronicConditions = false, IsActive = true, RegisteredByUserId = adminUser.UserId, CreatedAt = now.AddDays(-15), UpdatedAt = now
            },
            new()
            {
                PatientId = Guid.NewGuid(), TenantId = tenantId, MedicalRecordNumber = "KC-2026-00006",
                FirstName = "Emmanuel", LastName = "Acheampong", DateOfBirth = new DateOnly(2002, 1, 10), Gender = "Male",
                BloodType = "AB+", Email = "e.acheampong@example.com", PhoneNumber = "+233209887766", Country = "GH",
                AddressLine1 = "5 Spintex Road, Teshie, Accra", EmergencyContactName = "Francis Acheampong", EmergencyContactPhone = "+233244990011",
                HasAllergies = false, HasChronicConditions = false, IsActive = true, RegisteredByUserId = adminUser.UserId, CreatedAt = now.AddDays(-10), UpdatedAt = now
            },
            new()
            {
                PatientId = Guid.NewGuid(), TenantId = tenantId, MedicalRecordNumber = "KC-2026-00007",
                FirstName = "Grace", LastName = "Addo", DateOfBirth = new DateOnly(1981, 8, 30), Gender = "Female",
                BloodType = "A-", Email = "grace.addo@example.com", PhoneNumber = "+233541122445", Country = "GH",
                AddressLine1 = "77 Roman Ridge, Accra", EmergencyContactName = "Peter Addo", EmergencyContactPhone = "+233244883322",
                HasAllergies = true, HasChronicConditions = true, IsActive = true, RegisteredByUserId = adminUser.UserId, CreatedAt = now.AddDays(-7), UpdatedAt = now
            },
            new()
            {
                PatientId = Guid.NewGuid(), TenantId = tenantId, MedicalRecordNumber = "KC-2026-00008",
                FirstName = "Kojo", LastName = "Baah", DateOfBirth = new DateOnly(1995, 12, 5), Gender = "Male",
                BloodType = "O+", Email = "kojo.baah@example.com", PhoneNumber = "+233246778899", Country = "GH",
                AddressLine1 = "18 Labone Bypass, Accra", EmergencyContactName = "Esi Baah", EmergencyContactPhone = "+233201122334",
                HasAllergies = false, HasChronicConditions = false, IsActive = true, RegisteredByUserId = adminUser.UserId, CreatedAt = now.AddDays(-3), UpdatedAt = now
            }
        };

        foreach (var p in patientsToSeed)
        {
            var exists = await db.Patients.IgnoreQueryFilters().AnyAsync(pt => pt.TenantId == tenantId && pt.MedicalRecordNumber == p.MedicalRecordNumber);
            if (!exists) db.Patients.Add(p);
        }
        await db.SaveChangesAsync();

        var kwame = await db.Patients.IgnoreQueryFilters().FirstAsync(p => p.TenantId == tenantId && p.MedicalRecordNumber == "KC-2026-00001");
        var abena = await db.Patients.IgnoreQueryFilters().FirstAsync(p => p.TenantId == tenantId && p.MedicalRecordNumber == "KC-2026-00002");
        var kofi = await db.Patients.IgnoreQueryFilters().FirstAsync(p => p.TenantId == tenantId && p.MedicalRecordNumber == "KC-2026-00003");
        var yaw = await db.Patients.IgnoreQueryFilters().FirstAsync(p => p.TenantId == tenantId && p.MedicalRecordNumber == "KC-2026-00004");
        var akua = await db.Patients.IgnoreQueryFilters().FirstAsync(p => p.TenantId == tenantId && p.MedicalRecordNumber == "KC-2026-00005");
        var emmanuel = await db.Patients.IgnoreQueryFilters().FirstAsync(p => p.TenantId == tenantId && p.MedicalRecordNumber == "KC-2026-00006");
        var grace = await db.Patients.IgnoreQueryFilters().FirstAsync(p => p.TenantId == tenantId && p.MedicalRecordNumber == "KC-2026-00007");

        var kwameAllergy = await db.PatientAllergies.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.TenantId == tenantId && a.PatientId == kwame.PatientId);
        if (kwameAllergy is null)
        {
            db.PatientAllergies.Add(new PatientAllergy { AllergyId = Guid.NewGuid(), TenantId = tenantId, PatientId = kwame.PatientId, AllergyType = "Drug", AllergenName = "Penicillin", Severity = "Severe", Reaction = "Anaphylactic Rash & Bronchospasm", RecordedAt = now.AddMonths(-12), RecordedByUserId = adminUser.UserId });
        }

        var yawAllergy = await db.PatientAllergies.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.TenantId == tenantId && a.PatientId == yaw.PatientId);
        if (yawAllergy is null)
        {
            db.PatientAllergies.Add(new PatientAllergy { AllergyId = Guid.NewGuid(), TenantId = tenantId, PatientId = yaw.PatientId, AllergyType = "Drug", AllergenName = "Sulfonamides (Sulfa drugs)", Severity = "Moderate", Reaction = "Urticaria / Hives", RecordedAt = now.AddMonths(-6), RecordedByUserId = adminUser.UserId });
        }

        var graceAllergy = await db.PatientAllergies.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.TenantId == tenantId && a.PatientId == grace.PatientId);
        if (graceAllergy is null)
        {
            db.PatientAllergies.Add(new PatientAllergy { AllergyId = Guid.NewGuid(), TenantId = tenantId, PatientId = grace.PatientId, AllergyType = "Environmental", AllergenName = "House Dust Mites & Pollen", Severity = "Mild", Reaction = "Asthmatic Wheezing", RecordedAt = now.AddMonths(-24), RecordedByUserId = adminUser.UserId });
        }
        await db.SaveChangesAsync();

        // ─────────────────────────────────────────────────────────────────
        // 6. WARDS & BEDS
        // ─────────────────────────────────────────────────────────────────
        var wardA = await db.Wards.IgnoreQueryFilters().FirstOrDefaultAsync(w => w.TenantId == tenantId && w.Name == "Male Medical Ward A");
        if (wardA is null)
        {
            wardA = new Ward { WardId = Guid.NewGuid(), TenantId = tenantId, Name = "Male Medical Ward A", WardType = "Medical", Description = "Inpatient male general medical care ward", DailyRate = 120.00m, IsActive = true, CreatedAt = now, UpdatedAt = now };
            db.Wards.Add(wardA);
            await db.SaveChangesAsync();

            for (int i = 1; i <= 6; i++)
            {
                db.Beds.Add(new Bed { BedId = Guid.NewGuid(), TenantId = tenantId, WardId = wardA.WardId, BedNumber = $"BED-M{i:D2}", Status = BedStatus.Available, CreatedAt = now, UpdatedAt = now });
            }
            await db.SaveChangesAsync();
        }

        var wardB = await db.Wards.IgnoreQueryFilters().FirstOrDefaultAsync(w => w.TenantId == tenantId && w.Name == "Female Surgical Ward B");
        if (wardB is null)
        {
            wardB = new Ward { WardId = Guid.NewGuid(), TenantId = tenantId, Name = "Female Surgical Ward B", WardType = "Surgical", Description = "Post-operative & inpatient surgical care ward", DailyRate = 150.00m, IsActive = true, CreatedAt = now, UpdatedAt = now };
            db.Wards.Add(wardB);
            await db.SaveChangesAsync();

            for (int i = 1; i <= 6; i++)
            {
                db.Beds.Add(new Bed { BedId = Guid.NewGuid(), TenantId = tenantId, WardId = wardB.WardId, BedNumber = $"BED-F{i:D2}", Status = BedStatus.Available, CreatedAt = now, UpdatedAt = now });
            }
            await db.SaveChangesAsync();
        }

        var wardC = await db.Wards.IgnoreQueryFilters().FirstOrDefaultAsync(w => w.TenantId == tenantId && w.Name == "Pediatric Ward C");
        if (wardC is null)
        {
            wardC = new Ward { WardId = Guid.NewGuid(), TenantId = tenantId, Name = "Pediatric Ward C", WardType = "Pediatric", Description = "Pediatric inpatient and high-dependency unit", DailyRate = 100.00m, IsActive = true, CreatedAt = now, UpdatedAt = now };
            db.Wards.Add(wardC);
            await db.SaveChangesAsync();

            for (int i = 1; i <= 4; i++)
            {
                db.Beds.Add(new Bed { BedId = Guid.NewGuid(), TenantId = tenantId, WardId = wardC.WardId, BedNumber = $"BED-P{i:D2}", Status = BedStatus.Available, CreatedAt = now, UpdatedAt = now });
            }
            await db.SaveChangesAsync();
        }

        var bedM02 = await db.Beds.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.TenantId == tenantId && b.BedNumber == "BED-M02");
        var bedF03 = await db.Beds.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.TenantId == tenantId && b.BedNumber == "BED-F03");

        // ─────────────────────────────────────────────────────────────────
        // 7. INPATIENT ADMISSIONS
        // ─────────────────────────────────────────────────────────────────
        if (bedM02 != null)
        {
            var yawAdmission = await db.Admissions.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.TenantId == tenantId && a.PatientId == yaw.PatientId);
            if (yawAdmission is null)
            {
                yawAdmission = new Admission
                {
                    AdmissionId = Guid.NewGuid(), TenantId = tenantId, AdmissionNumber = "ADM-2026-00001",
                    PatientId = yaw.PatientId, BedId = bedM02.BedId, WardId = wardA!.WardId,
                    AdmittingDoctorUserId = doctorUser1!.UserId, CreatedByUserId = receptionist!.UserId,
                    AdmissionDate = now.AddDays(-5), ExpectedDischargeDate = now.AddDays(2), Status = AdmissionStatus.Active,
                    AdmissionReason = "Severe Acute Exacerbation of COPD with Secondary Chest Infection",
                    DiagnosisOnAdmission = "J44.1 - COPD with acute exacerbation",
                    CreatedAt = now.AddDays(-5), UpdatedAt = now
                };
                db.Admissions.Add(yawAdmission);
                bedM02.Status = BedStatus.Occupied;
            }
        }

        if (bedF03 != null)
        {
            var abenaAdmission = await db.Admissions.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.TenantId == tenantId && a.PatientId == abena.PatientId);
            if (abenaAdmission is null)
            {
                abenaAdmission = new Admission
                {
                    AdmissionId = Guid.NewGuid(), TenantId = tenantId, AdmissionNumber = "ADM-2026-00002",
                    PatientId = abena.PatientId, BedId = bedF03.BedId, WardId = wardB!.WardId,
                    AdmittingDoctorUserId = doctorUser3!.UserId, CreatedByUserId = receptionist!.UserId,
                    AdmissionDate = now.AddDays(-2), ExpectedDischargeDate = now.AddDays(3), Status = AdmissionStatus.Active,
                    AdmissionReason = "Elective Laparoscopic Appendectomy Post-Op Observation",
                    DiagnosisOnAdmission = "K35.80 - Unspecified acute appendicitis",
                    CreatedAt = now.AddDays(-2), UpdatedAt = now
                };
                db.Admissions.Add(abenaAdmission);
                bedF03.Status = BedStatus.Occupied;
            }
        }
        await db.SaveChangesAsync();

        // ─────────────────────────────────────────────────────────────────
        // 8. SUPPLIERS & DRUG INVENTORY
        // ─────────────────────────────────────────────────────────────────
        var supplierTobinco = await db.Suppliers.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Name == "Tobinco Pharmaceuticals Ltd");
        if (supplierTobinco is null)
        {
            supplierTobinco = new Supplier { SupplierId = Guid.NewGuid(), TenantId = tenantId, Name = "Tobinco Pharmaceuticals Ltd", ContactName = "Kwame Asante", Phone = "+233 30 222 1100", Email = "orders@tobinco.com", IsActive = true, CreatedAt = now, UpdatedAt = now };
            db.Suppliers.Add(supplierTobinco);
        }

        var supplierEntrance = await db.Suppliers.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Name == "Entrance Pharmaceuticals");
        if (supplierEntrance is null)
        {
            supplierEntrance = new Supplier { SupplierId = Guid.NewGuid(), TenantId = tenantId, Name = "Entrance Pharmaceuticals", ContactName = "Gladys Osei", Phone = "+233 30 281 5544", Email = "supply@entrancepharma.com", IsActive = true, CreatedAt = now, UpdatedAt = now };
            db.Suppliers.Add(supplierEntrance);
        }
        await db.SaveChangesAsync();

        var drugsToSeed = new List<DrugInventory>
        {
            new() { DrugInventoryId = Guid.NewGuid(), TenantId = tenantId, Name = "Coartem (Artemether-Lumefantrine)", GenericName = "Artemether + Lumefantrine", DosageForm = "Tablet", Strength = "20mg/120mg", Unit = "Blister", Category = "Antimalarial", CurrentStock = 450, ReorderThreshold = 100, UnitCost = 18.50m, SellingPrice = 35.00m, IsControlledSubstance = false, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { DrugInventoryId = Guid.NewGuid(), TenantId = tenantId, Name = "Paracetamol 500mg", GenericName = "Acetaminophen", DosageForm = "Tablet", Strength = "500mg", Unit = "Tablet", Category = "Analgesic / Antipyretic", CurrentStock = 2800, ReorderThreshold = 500, UnitCost = 0.15m, SellingPrice = 0.50m, IsControlledSubstance = false, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { DrugInventoryId = Guid.NewGuid(), TenantId = tenantId, Name = "Amoxicillin 500mg", GenericName = "Amoxicillin Trihydrate", DosageForm = "Capsule", Strength = "500mg", Unit = "Capsule", Category = "Antibiotic", CurrentStock = 1200, ReorderThreshold = 300, UnitCost = 0.80m, SellingPrice = 2.00m, IsControlledSubstance = false, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { DrugInventoryId = Guid.NewGuid(), TenantId = tenantId, Name = "Metformin 500mg", GenericName = "Metformin Hydrochloride", DosageForm = "Tablet", Strength = "500mg", Unit = "Tablet", Category = "Antidiabetic", CurrentStock = 950, ReorderThreshold = 200, UnitCost = 0.40m, SellingPrice = 1.20m, IsControlledSubstance = false, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { DrugInventoryId = Guid.NewGuid(), TenantId = tenantId, Name = "Amlodipine 10mg", GenericName = "Amlodipine Besylate", DosageForm = "Tablet", Strength = "10mg", Unit = "Tablet", Category = "Antihypertensive", CurrentStock = 650, ReorderThreshold = 150, UnitCost = 0.60m, SellingPrice = 1.80m, IsControlledSubstance = false, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { DrugInventoryId = Guid.NewGuid(), TenantId = tenantId, Name = "Omeprazole 20mg", GenericName = "Omeprazole", DosageForm = "Capsule", Strength = "20mg", Unit = "Capsule", Category = "Gastrointestinal", CurrentStock = 500, ReorderThreshold = 100, UnitCost = 1.10m, SellingPrice = 3.00m, IsControlledSubstance = false, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { DrugInventoryId = Guid.NewGuid(), TenantId = tenantId, Name = "IV Normal Saline 0.9%", GenericName = "Sodium Chloride 0.9%", DosageForm = "Infusion Bottle", Strength = "500ml", Unit = "Bottle", Category = "IV Fluids", CurrentStock = 180, ReorderThreshold = 50, UnitCost = 9.00m, SellingPrice = 20.00m, IsControlledSubstance = false, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { DrugInventoryId = Guid.NewGuid(), TenantId = tenantId, Name = "Salbutamol Inhaler", GenericName = "Salbutamol Sulfate", DosageForm = "Inhaler", Strength = "100mcg/dose", Unit = "Canister", Category = "Respiratory / Bronchodilator", CurrentStock = 45, ReorderThreshold = 15, UnitCost = 35.00m, SellingPrice = 65.00m, IsControlledSubstance = false, IsActive = true, CreatedAt = now, UpdatedAt = now }
        };

        foreach (var drug in drugsToSeed)
        {
            var exists = await db.DrugInventory.IgnoreQueryFilters().AnyAsync(d => d.TenantId == tenantId && d.Name == drug.Name);
            if (!exists)
            {
                db.DrugInventory.Add(drug);
                db.StockMovements.Add(new StockMovement
                {
                    StockMovementId = Guid.NewGuid(), TenantId = tenantId, DrugInventoryId = drug.DrugInventoryId,
                    MovementType = "StockIn", Quantity = drug.CurrentStock, PreviousStock = 0, NewStock = drug.CurrentStock,
                    Notes = "Initial system stock load", CreatedByUserId = pharmacist!.UserId, CreatedAt = now.AddDays(-30)
                });
            }
        }
        await db.SaveChangesAsync();

        // ─────────────────────────────────────────────────────────────────
        // 9. APPOINTMENTS & CONSULTATIONS
        // ─────────────────────────────────────────────────────────────────
        var appt1 = new Appointment
        {
            AppointmentId = Guid.NewGuid(), TenantId = tenantId, PatientId = kwame.PatientId, DoctorUserId = doctorUser1!.UserId,
            ScheduledAt = now.AddDays(-14).AddHours(9), DurationMinutes = 30, AppointmentType = "FollowUp", Status = "Completed",
            ChiefComplaint = "Routine BP Check & Refill", Room = "Consultation Room 1", CreatedByUserId = receptionist!.UserId, CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-14)
        };

        var appt2 = new Appointment
        {
            AppointmentId = Guid.NewGuid(), TenantId = tenantId, PatientId = kofi.PatientId, DoctorUserId = doctorUser1.UserId,
            ScheduledAt = now.AddDays(-10).AddHours(10), DurationMinutes = 30, AppointmentType = "General", Status = "Completed",
            ChiefComplaint = "Frequent urination and excessive thirst", Room = "Consultation Room 1", CreatedByUserId = receptionist.UserId, CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-10)
        };

        var appt3 = new Appointment
        {
            AppointmentId = Guid.NewGuid(), TenantId = tenantId, PatientId = akua.PatientId, DoctorUserId = doctorUser2!.UserId,
            ScheduledAt = now.AddDays(-5).AddHours(11), DurationMinutes = 30, AppointmentType = "Emergency", Status = "Completed",
            ChiefComplaint = "High fever, vomiting and lethargy", Room = "Pediatric Clinic Room 3", CreatedByUserId = receptionist.UserId, CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-5)
        };

        var appt4 = new Appointment
        {
            AppointmentId = Guid.NewGuid(), TenantId = tenantId, PatientId = grace.PatientId, DoctorUserId = doctorUser1.UserId,
            ScheduledAt = now.AddDays(-2).AddHours(14), DurationMinutes = 30, AppointmentType = "General", Status = "Completed",
            ChiefComplaint = "Shortness of breath and persistent nighttime cough", Room = "Consultation Room 1", CreatedByUserId = receptionist.UserId, CreatedAt = now.AddDays(-2), UpdatedAt = now.AddDays(-2)
        };

        var appt5 = new Appointment
        {
            AppointmentId = Guid.NewGuid(), TenantId = tenantId, PatientId = emmanuel.PatientId, DoctorUserId = doctorUser3!.UserId,
            ScheduledAt = now.AddHours(1), DurationMinutes = 30, AppointmentType = "General", Status = "CheckedIn",
            ChiefComplaint = "Right ankle swelling post soccer injury", Room = "Surgical OPD Room 2", CreatedByUserId = receptionist.UserId, CreatedAt = now, UpdatedAt = now
        };

        var appt6 = new Appointment
        {
            AppointmentId = Guid.NewGuid(), TenantId = tenantId, PatientId = kwame.PatientId, DoctorUserId = doctorUser1.UserId,
            ScheduledAt = now.AddDays(3).AddHours(10), DurationMinutes = 30, AppointmentType = "FollowUp", Status = "Scheduled",
            ChiefComplaint = "Monthly Hypertension Review", Room = "Consultation Room 1", CreatedByUserId = receptionist.UserId, CreatedAt = now, UpdatedAt = now
        };

        var appointmentsToSeed = new[] { appt1, appt2, appt3, appt4, appt5, appt6 };
        foreach (var appt in appointmentsToSeed)
        {
            var exists = await db.Appointments.IgnoreQueryFilters().AnyAsync(a => a.TenantId == tenantId && a.PatientId == appt.PatientId && a.ScheduledAt == appt.ScheduledAt);
            if (!exists) db.Appointments.Add(appt);
        }
        await db.SaveChangesAsync();

        var consult1 = await db.Consultations.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.TenantId == tenantId && c.AppointmentId == appt1.AppointmentId);
        if (consult1 is null)
        {
            consult1 = new Consultation
            {
                ConsultationId = Guid.NewGuid(), TenantId = tenantId, AppointmentId = appt1.AppointmentId, PatientId = kwame.PatientId, DoctorUserId = doctorUser1.UserId,
                SubjectiveNotes = "Patient reports mild headache in the mornings. Adhering to Amlodipine 10mg daily.",
                ObjectiveNotes = "Alert, oriented x3. Normal heart sounds. No pedaloedema.",
                AssessmentNotes = "Essential Primary Hypertension - Fairly Controlled.",
                PlanNotes = "Continue Amlodipine 10mg daily. Low salt diet. Review in 1 month.",
                BloodPressureSystolic = 138, BloodPressureDiastolic = 88, HeartRateBPM = 74, TemperatureCelsius = 36.6m, WeightKg = 82.5m, HeightCm = 175.0m, OxygenSaturationPct = 98m,
                PrimaryDiagnosisCode = "I10", PrimaryDiagnosisDesc = "Essential Primary Hypertension", SecondaryDiagnoses = "[]",
                Status = "Signed", SignedAt = appt1.ScheduledAt.AddMinutes(25), CreatedAt = appt1.ScheduledAt, UpdatedAt = appt1.ScheduledAt
            };
            db.Consultations.Add(consult1);
        }

        var consult2 = await db.Consultations.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.TenantId == tenantId && c.AppointmentId == appt2.AppointmentId);
        if (consult2 is null)
        {
            consult2 = new Consultation
            {
                ConsultationId = Guid.NewGuid(), TenantId = tenantId, AppointmentId = appt2.AppointmentId, PatientId = kofi.PatientId, DoctorUserId = doctorUser1.UserId,
                SubjectiveNotes = "2-week history of polyuria, polydipsia, and fatigue. No previous diabetic diagnosis.",
                ObjectiveNotes = "Bilateral clear lungs. Abdomen soft, non-tender. Random Blood Glucose = 16.4 mmol/L.",
                AssessmentNotes = "New onset Type 2 Diabetes Mellitus with hyperglycemia.",
                PlanNotes = "Start Metformin 500mg BD. Order FBG, HbA1c and BUE & Creatinine. Dietary counseling.",
                BloodPressureSystolic = 142, BloodPressureDiastolic = 90, HeartRateBPM = 82, TemperatureCelsius = 36.8m, WeightKg = 91.0m, HeightCm = 170.0m, OxygenSaturationPct = 97m,
                PrimaryDiagnosisCode = "E11.9", PrimaryDiagnosisDesc = "Type 2 Diabetes Mellitus without complications", SecondaryDiagnoses = "[]",
                Status = "Signed", SignedAt = appt2.ScheduledAt.AddMinutes(30), CreatedAt = appt2.ScheduledAt, UpdatedAt = appt2.ScheduledAt
            };
            db.Consultations.Add(consult2);
        }

        var consult3 = await db.Consultations.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.TenantId == tenantId && c.AppointmentId == appt3.AppointmentId);
        if (consult3 is null)
        {
            consult3 = new Consultation
            {
                ConsultationId = Guid.NewGuid(), TenantId = tenantId, AppointmentId = appt3.AppointmentId, PatientId = akua.PatientId, DoctorUserId = doctorUser2.UserId,
                SubjectiveNotes = "Mother reports 3-day history of high grade fever (39.2C), Poor oral intake and 2 episodes of non-bilious vomiting.",
                ObjectiveNotes = "Irritable child, febrile to touch. Mild palmar pallor. Splenomegaly 2cm below costal margin.",
                AssessmentNotes = "Uncomplicated Plasmodium falciparum Malaria.",
                PlanNotes = "Order immediate Blood Film for MP. Administer oral Coartem suspension. Paracetamol syrup for fever.",
                BloodPressureSystolic = 95, BloodPressureDiastolic = 60, HeartRateBPM = 124, TemperatureCelsius = 39.2m, WeightKg = 14.5m, HeightCm = 98.0m, OxygenSaturationPct = 96m,
                PrimaryDiagnosisCode = "B54", PrimaryDiagnosisDesc = "Unspecified Malaria", SecondaryDiagnoses = "[]",
                Status = "Signed", SignedAt = appt3.ScheduledAt.AddMinutes(20), CreatedAt = appt3.ScheduledAt, UpdatedAt = appt3.ScheduledAt
            };
            db.Consultations.Add(consult3);
        }
        await db.SaveChangesAsync();

        // ─────────────────────────────────────────────────────────────────
        // 10. VITAL SIGNS & NURSING NOTES
        // ─────────────────────────────────────────────────────────────────
        var vitalsToSeed = new List<VitalSigns>
        {
            new() { VitalSignsId = Guid.NewGuid(), TenantId = tenantId, PatientId = kwame.PatientId, RecordedByUserId = nurseUser1!.UserId, RecordedAt = now.AddDays(-14).AddMinutes(-10), BloodPressureSystolic = 140, BloodPressureDiastolic = 90, PulseRate = 76, Temperature = 36.5m, SpO2 = 99, RespiratoryRate = 16, Weight = 82.5m, Height = 175m, Notes = "OPD Triage Vitals", CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-14) },
            new() { VitalSignsId = Guid.NewGuid(), TenantId = tenantId, PatientId = kofi.PatientId, RecordedByUserId = nurseUser1.UserId, RecordedAt = now.AddDays(-10).AddMinutes(-10), BloodPressureSystolic = 144, BloodPressureDiastolic = 92, PulseRate = 84, Temperature = 36.8m, SpO2 = 97, RespiratoryRate = 18, Weight = 91.0m, Height = 170m, Notes = "OPD Routine Vitals", CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-10) },
            new() { VitalSignsId = Guid.NewGuid(), TenantId = tenantId, PatientId = akua.PatientId, RecordedByUserId = nurseUser1.UserId, RecordedAt = now.AddDays(-5).AddMinutes(-10), BloodPressureSystolic = 95, BloodPressureDiastolic = 60, PulseRate = 128, Temperature = 39.2m, SpO2 = 96, RespiratoryRate = 28, Weight = 14.5m, Height = 98m, Notes = "Pediatric High Fever Triage", CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-5) },
            new() { VitalSignsId = Guid.NewGuid(), TenantId = tenantId, PatientId = yaw.PatientId, RecordedByUserId = nurseUser2!.UserId, RecordedAt = now.AddHours(-4), BloodPressureSystolic = 132, BloodPressureDiastolic = 82, PulseRate = 80, Temperature = 37.1m, SpO2 = 95, RespiratoryRate = 20, Weight = 74.0m, Height = 168m, Notes = "Ward Morning Vitals Round", CreatedAt = now.AddHours(-4), UpdatedAt = now.AddHours(-4) }
        };

        foreach (var v in vitalsToSeed)
        {
            var exists = await db.VitalSigns.IgnoreQueryFilters().AnyAsync(vs => vs.TenantId == tenantId && vs.PatientId == v.PatientId && vs.RecordedAt == v.RecordedAt);
            if (!exists) db.VitalSigns.Add(v);
        }
        await db.SaveChangesAsync();

        var nurseNote = await db.NursingNotes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.TenantId == tenantId && n.PatientId == yaw.PatientId);
        if (nurseNote is null)
        {
            db.NursingNotes.Add(new NursingNote
            {
                NursingNoteId = Guid.NewGuid(), TenantId = tenantId, PatientId = yaw.PatientId, AdmissionId = (await db.Admissions.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.PatientId == yaw.PatientId))?.AdmissionId,
                AuthorId = nurseUser2.UserId, NoteType = "ShiftHandover", Note = "Patient slept comfortably through the night. Nebulized with Salbutamol at 02:00. SpO2 stable at 95% on room air.",
                CreatedAt = now.AddHours(-6), UpdatedAt = now.AddHours(-6)
            });
            await db.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────────────
        // 11. LABORATORY ORDERS & RESULTS
        // ─────────────────────────────────────────────────────────────────
        var fbcCatalog = await db.LabTestCatalog.FirstOrDefaultAsync(c => c.TestCode == "FBC");
        var mpsCatalog = await db.LabTestCatalog.FirstOrDefaultAsync(c => c.TestCode == "MPS");
        var bueCatalog = await db.LabTestCatalog.FirstOrDefaultAsync(c => c.TestCode == "BUE");

        var labOrder1 = await db.LabOrders.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.TenantId == tenantId && l.PatientId == akua.PatientId);
        if (labOrder1 is null && mpsCatalog != null && fbcCatalog != null)
        {
            labOrder1 = new LabOrder
            {
                LabOrderId = Guid.NewGuid(), TenantId = tenantId, PatientId = akua.PatientId, ConsultationId = consult3?.ConsultationId,
                OrderingDoctorUserId = doctorUser2.UserId, Organisation = "DIRECT", Status = "Completed", Notes = "STAT Malaria screen for high fever",
                CreatedAt = now.AddDays(-5).AddMinutes(15), UpdatedAt = now.AddDays(-5).AddMinutes(60)
            };
            db.LabOrders.Add(labOrder1);

            var itemMps = new LabOrderItem
            {
                LabOrderItemId = Guid.NewGuid(), LabOrderId = labOrder1.LabOrderId, TenantId = tenantId, LabTestCatalogId = mpsCatalog.LabTestCatalogId,
                TestName = "Blood Film for Malaria Parasite Screen", Department = "Haematology", IsManualEntry = true, TatHours = 2,
                AccessionNumber = "ACC-2026-00088", Status = "Signed", SampleReceivedAt = now.AddDays(-5).AddMinutes(25), ResultedAt = now.AddDays(-5).AddMinutes(45),
                SignedAt = now.AddDays(-5).AddMinutes(50), SignedByUserId = labTech!.UserId, ManualResult = "POSITIVE (3+ Plasmodium falciparum trophozoites)",
                ManualResultNotes = "High parasitaemia noted (>10,000 parasites/uL)", ManualResultFlag = "H", IsCritical = true
            };
            db.LabOrderItems.Add(itemMps);

            var criticalLog = new CriticalCallLog
            {
                CriticalCallLogId = Guid.NewGuid(), TenantId = tenantId, LabOrderItemId = itemMps.LabOrderItemId,
                RecipientName = "Dr. Efua Mensah", CalledByName = "Yaw Dankwa (Lab Tech)", CalledAt = now.AddDays(-5).AddMinutes(48),
                Notes = "Notified Dr. Mensah immediately of 3+ P. falciparum parasitaemia in 4yo child.", CreatedAt = now.AddDays(-5).AddMinutes(48), UpdatedAt = now.AddDays(-5).AddMinutes(48)
            };
            db.CriticalCallLogs.Add(criticalLog);
            itemMps.CriticalCallLogId = criticalLog.CriticalCallLogId;
        }

        var labOrder2 = await db.LabOrders.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.TenantId == tenantId && l.PatientId == kofi.PatientId);
        if (labOrder2 is null && bueCatalog != null)
        {
            labOrder2 = new LabOrder
            {
                LabOrderId = Guid.NewGuid(), TenantId = tenantId, PatientId = kofi.PatientId, ConsultationId = consult2?.ConsultationId,
                OrderingDoctorUserId = doctorUser1.UserId, Organisation = "DIRECT", Status = "Completed", Notes = "Baseline renal function for diabetic review",
                CreatedAt = now.AddDays(-10).AddMinutes(20), UpdatedAt = now.AddDays(-10).AddMinutes(90)
            };
            db.LabOrders.Add(labOrder2);

            var itemBue = new LabOrderItem
            {
                LabOrderItemId = Guid.NewGuid(), LabOrderId = labOrder2.LabOrderId, TenantId = tenantId, LabTestCatalogId = bueCatalog.LabTestCatalogId,
                TestName = "Blood Urea and Electrolytes & Creatinine", Department = "Chemistry", InstrumentType = "DxC500", IsManualEntry = false, TatHours = 3,
                AccessionNumber = "ACC-2026-00042", Status = "Signed", SampleReceivedAt = now.AddDays(-10).AddMinutes(30), ResultedAt = now.AddDays(-10).AddMinutes(75),
                SignedAt = now.AddDays(-10).AddMinutes(85), SignedByUserId = labTech!.UserId, IsCritical = false
            };
            db.LabOrderItems.Add(itemBue);

            var labResultBue = new LabResult
            {
                LabResultId = Guid.NewGuid(), TenantId = tenantId, PatientId = kofi.PatientId, OrderingDoctorUserId = doctorUser1.UserId,
                AccessionNumber = "ACC-2026-00042", OrderCode = "BUE", OrderName = "Blood Urea and Electrolytes", OrderedAt = labOrder2.CreatedAt,
                ReceivedAt = now.AddDays(-10).AddMinutes(75), Status = "Verified", LabOrderItemId = itemBue.LabOrderItemId, CreatedAt = now.AddDays(-10).AddMinutes(75), UpdatedAt = now.AddDays(-10).AddMinutes(85)
            };
            db.LabResults.Add(labResultBue);
            itemBue.LabResultId = labResultBue.LabResultId;

            db.LabObservations.AddRange(new[]
            {
                new LabObservation { LabObservationId = Guid.NewGuid(), LabResultId = labResultBue.LabResultId, TenantId = tenantId, SequenceNumber = 1, TestCode = "UREA", TestName = "Blood Urea", Value = "6.8", Units = "mmol/L", ReferenceRange = "2.5-7.5", AbnormalFlag = "N" },
                new LabObservation { LabObservationId = Guid.NewGuid(), LabResultId = labResultBue.LabResultId, TenantId = tenantId, SequenceNumber = 2, TestCode = "CREAT", TestName = "Serum Creatinine", Value = "98", Units = "umol/L", ReferenceRange = "60-110", AbnormalFlag = "N" },
                new LabObservation { LabObservationId = Guid.NewGuid(), LabResultId = labResultBue.LabResultId, TenantId = tenantId, SequenceNumber = 3, TestCode = "NA", TestName = "Sodium", Value = "139", Units = "mmol/L", ReferenceRange = "135-145", AbnormalFlag = "N" },
                new LabObservation { LabObservationId = Guid.NewGuid(), LabResultId = labResultBue.LabResultId, TenantId = tenantId, SequenceNumber = 4, TestCode = "K", TestName = "Potassium", Value = "4.3", Units = "mmol/L", ReferenceRange = "3.5-5.1", AbnormalFlag = "N" }
            });
        }
        await db.SaveChangesAsync();

        // ─────────────────────────────────────────────────────────────────
        // 12. RADIOLOGY ORDERS
        // ─────────────────────────────────────────────────────────────────
        var chestXrayProc = await db.ImagingProcedures.FirstOrDefaultAsync(p => p.ProcedureCode == "RAD-CXR-01") ?? new ImagingProcedure { ImagingProcedureId = Guid.NewGuid(), ProcedureCode = "RAD-CXR-01", ProcedureName = "Chest X-Ray PA View", Modality = "XR", BodyPart = "Chest", Department = "Radiology", TatHours = 4, IsActive = true };
        if (await db.ImagingProcedures.FirstOrDefaultAsync(p => p.ProcedureCode == "RAD-CXR-01") == null) db.ImagingProcedures.Add(chestXrayProc);
        await db.SaveChangesAsync();

        var radOrder1 = await db.RadiologyOrders.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.TenantId == tenantId && r.PatientId == grace.PatientId);
        if (radOrder1 is null)
        {
            radOrder1 = new RadiologyOrder
            {
                RadiologyOrderId = Guid.NewGuid(), TenantId = tenantId, PatientId = grace.PatientId, OrderingDoctorUserId = doctorUser1.UserId,
                Priority = "Routine", Status = "Completed", ClinicalIndication = "Persistent asthma exacerbation, rule out pneumonia", Notes = "Standard PA Chest", CreatedAt = now.AddDays(-2).AddMinutes(30), UpdatedAt = now.AddDays(-2).AddHours(3)
            };
            db.RadiologyOrders.Add(radOrder1);

            var radItem1 = new RadiologyOrderItem
            {
                RadiologyOrderItemId = Guid.NewGuid(), RadiologyOrderId = radOrder1.RadiologyOrderId, TenantId = tenantId, ImagingProcedureId = chestXrayProc.ImagingProcedureId,
                ProcedureName = "Chest X-Ray PA View", Modality = "XR", BodyPart = "Chest", Department = "Radiology", TatHours = 4, AccessionNumber = "RAD-2026-00019",
                Status = "Signed", AcquiredAt = now.AddDays(-2).AddHours(1), ReportedAt = now.AddDays(-2).AddHours(2), SignedAt = now.AddDays(-2).AddHours(3), SignedByUserId = doctorUser1.UserId,
                Findings = "Lungs are hyperinflated with increased bronchovascular markings. No focal consolidation, pleural effusion or pneumothorax.",
                Impression = "Features compatible with reactive airway disease / asthmatic bronchial changes. No acute parenchymal pneumonia.",
                Recommendations = "Clinical correlation advised.", ReportingDoctorUserId = doctorUser1.UserId
            };
            db.RadiologyOrderItems.Add(radItem1);
            await db.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────────────
        // 13. PRESCRIPTIONS & DISPENSING
        // ─────────────────────────────────────────────────────────────────
        var rx1 = await db.Prescriptions.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.TenantId == tenantId && p.PatientId == akua.PatientId);
        if (rx1 is null)
        {
            rx1 = new Prescription
            {
                PrescriptionId = Guid.NewGuid(), TenantId = tenantId, ConsultationId = consult3!.ConsultationId, PatientId = akua.PatientId, PrescribedByUserId = doctorUser2.UserId,
                PrescriptionDate = DateOnly.FromDateTime(now.AddDays(-5)), ExpiresAt = DateOnly.FromDateTime(now.AddDays(25)), Status = "Dispensed", DispensedAt = now.AddDays(-5).AddHours(2),
                DispensedByUserId = pharmacist!.UserId, Notes = "Complete full 3-day course of Coartem", CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-5).AddHours(2)
            };
            db.Prescriptions.Add(rx1);

            var rxItem1 = new PrescriptionItem
            {
                ItemId = Guid.NewGuid(), PrescriptionId = rx1.PrescriptionId, TenantId = tenantId, MedicationName = "Coartem (Artemether-Lumefantrine)", GenericName = "Artemether + Lumefantrine",
                Strength = "20mg/120mg", DosageForm = "Tablet", Frequency = "BD (Twice Daily)", DurationDays = 3, Quantity = 6, Refills = 0, Instructions = "Take with fatty food or milk",
                QuantityDispensed = 6, IsFullyDispensed = true
            };
            db.PrescriptionItems.Add(rxItem1);

            var dispense1 = new DispenseEvent
            {
                DispenseEventId = Guid.NewGuid(), TenantId = tenantId, PrescriptionId = rx1.PrescriptionId, DispensedByUserId = pharmacist.UserId, DispensedAt = now.AddDays(-5).AddHours(2),
                Notes = "Handed to mother with pediatric dosage instruction leaflet"
            };
            db.DispenseEvents.Add(dispense1);
            await db.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────────────
        // 14. BILLS, PAYMENTS & INSURANCE CLAIMS
        // ─────────────────────────────────────────────────────────────────
        var bill1 = await db.Bills.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.TenantId == tenantId && b.PatientId == akua.PatientId);
        if (bill1 is null)
        {
            bill1 = new Bill
            {
                BillId = Guid.NewGuid(), TenantId = tenantId, BillNumber = "INV-2026-00015", PatientId = akua.PatientId, ConsultationId = consult3!.ConsultationId, PayerId = selfPayer!.PayerId,
                CreatedByUserId = receptionist!.UserId, Status = "Paid", Notes = "Outpatient Consultation & Pharmacy Bill", TotalAmount = 115.00m, PaidAmount = 115.00m, BalanceDue = 0.00m,
                IssuedAt = now.AddDays(-5).AddMinutes(10), CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-5).AddHours(2)
            };
            db.Bills.Add(bill1);

            db.BillItems.AddRange(new[]
            {
                new BillItem { ItemId = Guid.NewGuid(), TenantId = tenantId, BillId = bill1.BillId, Description = "General OPD Consultation", Category = "Consultation", Quantity = 1, UnitPrice = 80.00m, TotalPrice = 80.00m, SourceType = "Consultation", SourceId = consult3.ConsultationId },
                new BillItem { ItemId = Guid.NewGuid(), TenantId = tenantId, BillId = bill1.BillId, Description = "Coartem 20/120mg x 6 tabs", Category = "Pharmacy", Quantity = 1, UnitPrice = 35.00m, TotalPrice = 35.00m, SourceType = "Prescription", SourceId = rx1.PrescriptionId }
            });

            var payment1 = new Payment
            {
                PaymentId = Guid.NewGuid(), TenantId = tenantId, BillId = bill1.BillId, ReceivedByUserId = billingOfficer!.UserId, Amount = 115.00m, PaymentMethod = "MTN Mobile Money",
                Reference = "MOMO-2026-991823", Notes = "MoMo Payment received from +233245667788", PaymentDate = now.AddDays(-5).AddHours(2), CreatedAt = now.AddDays(-5).AddHours(2), UpdatedAt = now.AddDays(-5).AddHours(2)
            };
            db.Payments.Add(payment1);
        }

        var bill2 = await db.Bills.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.TenantId == tenantId && b.PatientId == abena.PatientId);
        if (bill2 is null)
        {
            bill2 = new Bill
            {
                BillId = Guid.NewGuid(), TenantId = tenantId, BillNumber = "INV-2026-00022", PatientId = abena.PatientId, PayerId = nhisPayer!.PayerId,
                CreatedByUserId = billingOfficer!.UserId, Status = "Issued", Notes = "Inpatient Surgical Admission Bill - Pending NHIS Claim Approval", TotalAmount = 450.00m, PaidAmount = 0.00m, BalanceDue = 450.00m,
                IssuedAt = now.AddDays(-2), CreatedAt = now.AddDays(-2), UpdatedAt = now
            };
            db.Bills.Add(bill2);

            db.BillItems.AddRange(new[]
            {
                new BillItem { ItemId = Guid.NewGuid(), TenantId = tenantId, BillId = bill2.BillId, Description = "Daily Inpatient Accommodation (Female Surgical Ward B) x 2 Days", Category = "Inpatient", Quantity = 2, UnitPrice = 150.00m, TotalPrice = 300.00m },
                new BillItem { ItemId = Guid.NewGuid(), TenantId = tenantId, BillId = bill2.BillId, Description = "Post-Op Nursing Care x 2 Days", Category = "Nursing", Quantity = 2, UnitPrice = 50.00m, TotalPrice = 100.00m },
                new BillItem { ItemId = Guid.NewGuid(), TenantId = tenantId, BillId = bill2.BillId, Description = "IV Normal Saline 0.9% x 2", Category = "Pharmacy", Quantity = 2, UnitPrice = 25.00m, TotalPrice = 50.00m }
            });

            var claimNhis = new InsuranceClaim
            {
                ClaimId = Guid.NewGuid(), TenantId = tenantId, ClaimNumber = "CLM-2026-00008", BillId = bill2.BillId, PayerId = nhisPayer.PayerId, PatientId = abena.PatientId,
                CreatedByUserId = billingOfficer.UserId, NhisNumber = abena.NhisNumber, Status = ClaimStatus.Submitted, ClaimAmount = 450.00m, Notes = "Submitted to NHIS Portal for Surgical Ward stay",
                SubmittedAt = now.AddDays(-1), CreatedAt = now.AddDays(-1), UpdatedAt = now
            };
            db.InsuranceClaims.Add(claimNhis);
        }

        await db.SaveChangesAsync();

        logger.LogInformation("Comprehensive demo data successfully seeded into 'Demo Hospital'!");
    }
}
