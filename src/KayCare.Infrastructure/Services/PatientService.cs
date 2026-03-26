using KayCare.Core.Constants;
using KayCare.Core.DTOs.Common;
using KayCare.Core.DTOs.Patients;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Services;

public class PatientService : IPatientService
{
    private readonly AppDbContext        _db;
    private readonly ITenantContext      _tenantContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService       _audit;

    public PatientService(AppDbContext db, ITenantContext tenantContext, ICurrentUserService currentUser, IAuditService audit)
    {
        _db          = db;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _audit       = audit;
    }

    // ── Register ──────────────────────────────────────────────────────────────

    public async Task<PatientDetailResponse> RegisterAsync(CreatePatientRequest req, CancellationToken ct = default)
    {
        var mrn = await GenerateMrnAsync(ct);

        var patient = new Patient
        {
            MedicalRecordNumber      = mrn,
            FirstName                = req.FirstName.Trim(),
            MiddleName               = req.MiddleName?.Trim(),
            LastName                 = req.LastName.Trim(),
            DateOfBirth              = req.DateOfBirth,
            Gender                   = req.Gender,
            BloodType                = req.BloodType,
            NationalId               = req.NationalId,
            Email                    = req.Email?.Trim().ToLowerInvariant(),
            PhoneNumber              = req.PhoneNumber,
            AlternatePhone           = req.AlternatePhone,
            AddressLine1             = req.AddressLine1,
            AddressLine2             = req.AddressLine2,
            City                     = req.City,
            State                    = req.State,
            PostalCode               = req.PostalCode,
            Country                  = req.Country ?? "GH",
            EmergencyContactName     = req.EmergencyContactName,
            EmergencyContactPhone    = req.EmergencyContactPhone,
            EmergencyContactRelation = req.EmergencyContactRelation,
            InsuranceProvider        = req.InsuranceProvider,
            InsurancePolicyNumber    = req.InsurancePolicyNumber,
            InsuranceGroupNumber     = req.InsuranceGroupNumber,
            RegisteredByUserId       = _currentUser.UserId
        };

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.PatientCreate, nameof(Patient), patient.PatientId, patient.PatientId, ct: ct);

        return MapToDetail(patient);
    }

    // ── Search ────────────────────────────────────────────────────────────────

    public async Task<PagedResult<PatientResponse>> SearchAsync(PatientSearchRequest req, CancellationToken ct = default)
    {
        var query = _db.Patients.AsNoTracking().Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(req.Query))
        {
            var q = req.Query.Trim();
            // SQL Server uses CI_AS collation by default — Contains is case-insensitive
            query = query.Where(p =>
                p.FirstName.Contains(q) ||
                p.LastName.Contains(q) ||
                p.MedicalRecordNumber.Contains(q) ||
                (p.PhoneNumber != null && p.PhoneNumber.Contains(q)));
        }

        if (req.DateOfBirth.HasValue)
            query = query.Where(p => p.DateOfBirth == req.DateOfBirth.Value);

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync(ct);   // pagination in SQL; mapping in memory

        return new PagedResult<PatientResponse>
        {
            Items      = rows.Select(MapToSummary).ToList(),
            TotalCount = total,
            Page       = req.Page,
            PageSize   = req.PageSize
        };
    }

    // ── Get by ID ─────────────────────────────────────────────────────────────

    public async Task<PatientDetailResponse> GetByIdAsync(Guid patientId, CancellationToken ct = default)
    {
        var patient = await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PatientId == patientId, ct)
            ?? throw new NotFoundException(nameof(Patient), patientId);

        await _audit.LogAsync(AuditActions.PatientView, nameof(Patient), patientId, patientId, ct: ct);
        return MapToDetail(patient);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    public async Task<PatientDetailResponse> UpdateAsync(Guid patientId, UpdatePatientRequest req, CancellationToken ct = default)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.PatientId == patientId, ct)
            ?? throw new NotFoundException(nameof(Patient), patientId);

        if (req.FirstName is not null) patient.FirstName = req.FirstName.Trim();
        if (req.MiddleName is not null) patient.MiddleName = req.MiddleName.Trim();
        if (req.LastName is not null) patient.LastName = req.LastName.Trim();
        if (req.BloodType is not null) patient.BloodType = req.BloodType;
        if (req.NationalId is not null) patient.NationalId = req.NationalId;
        if (req.Email is not null) patient.Email = req.Email.Trim().ToLowerInvariant();
        if (req.PhoneNumber is not null) patient.PhoneNumber = req.PhoneNumber;
        if (req.AlternatePhone is not null) patient.AlternatePhone = req.AlternatePhone;
        if (req.AddressLine1 is not null) patient.AddressLine1 = req.AddressLine1;
        if (req.AddressLine2 is not null) patient.AddressLine2 = req.AddressLine2;
        if (req.City is not null) patient.City = req.City;
        if (req.State is not null) patient.State = req.State;
        if (req.PostalCode is not null) patient.PostalCode = req.PostalCode;
        if (req.Country is not null) patient.Country = req.Country;
        if (req.EmergencyContactName is not null) patient.EmergencyContactName = req.EmergencyContactName;
        if (req.EmergencyContactPhone is not null) patient.EmergencyContactPhone = req.EmergencyContactPhone;
        if (req.EmergencyContactRelation is not null) patient.EmergencyContactRelation = req.EmergencyContactRelation;
        if (req.InsuranceProvider is not null) patient.InsuranceProvider = req.InsuranceProvider;
        if (req.InsurancePolicyNumber is not null) patient.InsurancePolicyNumber = req.InsurancePolicyNumber;
        if (req.InsuranceGroupNumber is not null) patient.InsuranceGroupNumber = req.InsuranceGroupNumber;
        if (req.HasChronicConditions.HasValue) patient.HasChronicConditions = req.HasChronicConditions.Value;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.PatientUpdate, nameof(Patient), patientId, patientId, ct: ct);

        return MapToDetail(patient);
    }

    // ── Allergies ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<AllergyResponse>> GetAllergiesAsync(Guid patientId, CancellationToken ct = default)
    {
        await EnsurePatientExistsAsync(patientId, ct);

        return await _db.PatientAllergies
            .AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .OrderBy(a => a.RecordedAt)
            .Select(a => MapToAllergyResponse(a))
            .ToListAsync(ct);
    }

    public async Task<AllergyResponse> AddAllergyAsync(Guid patientId, AddAllergyRequest req, CancellationToken ct = default)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.PatientId == patientId, ct)
            ?? throw new NotFoundException(nameof(Patient), patientId);

        var allergy = new PatientAllergy
        {
            TenantId          = _tenantContext.TenantId,
            PatientId         = patientId,
            AllergyType       = req.AllergyType,
            AllergenName      = req.AllergenName,
            Reaction          = req.Reaction,
            Severity          = req.Severity,
            RecordedAt        = DateTime.UtcNow,
            RecordedByUserId  = _currentUser.UserId
        };

        patient.HasAllergies = true;

        _db.PatientAllergies.Add(allergy);
        await _db.SaveChangesAsync(ct);

        return MapToAllergyResponse(allergy);
    }

    public async Task RemoveAllergyAsync(Guid patientId, Guid allergyId, CancellationToken ct = default)
    {
        var allergy = await _db.PatientAllergies
            .FirstOrDefaultAsync(a => a.PatientId == patientId && a.AllergyId == allergyId, ct)
            ?? throw new NotFoundException(nameof(PatientAllergy), allergyId);

        _db.PatientAllergies.Remove(allergy);

        // Update HasAllergies flag if no allergies remain
        var remaining = await _db.PatientAllergies
            .CountAsync(a => a.PatientId == patientId && a.AllergyId != allergyId, ct);

        if (remaining == 0)
        {
            var patient = await _db.Patients.FirstOrDefaultAsync(p => p.PatientId == patientId, ct);
            if (patient is not null) patient.HasAllergies = false;
        }

        await _db.SaveChangesAsync(ct);
    }

    // ── MRN Generation ────────────────────────────────────────────────────────

    private async Task<string> GenerateMrnAsync(CancellationToken ct)
    {
        var year   = DateTime.UtcNow.Year;
        var prefix = $"MRN-{year}-";

        var lastMrn = await _db.Patients
            .Where(p => p.MedicalRecordNumber.StartsWith(prefix))
            .OrderByDescending(p => p.MedicalRecordNumber)
            .Select(p => p.MedicalRecordNumber)
            .FirstOrDefaultAsync(ct);

        var seq = 1;
        if (lastMrn is not null &&
            int.TryParse(lastMrn[prefix.Length..], out var last))
        {
            seq = last + 1;
        }

        return $"{prefix}{seq:D5}";
    }

    // ── Mapping helpers ───────────────────────────────────────────────────────

    private static PatientResponse MapToSummary(Patient p) => new()
    {
        PatientId           = p.PatientId,
        MedicalRecordNumber = p.MedicalRecordNumber,
        FullName            = $"{p.FirstName} {p.LastName}".Trim(),
        DateOfBirth         = p.DateOfBirth,
        Age                 = CalculateAge(p.DateOfBirth),
        Gender              = p.Gender,
        PhoneNumber         = p.PhoneNumber,
        HasAllergies        = p.HasAllergies,
        IsActive            = p.IsActive,
        RegisteredAt        = p.CreatedAt
    };

    private static PatientDetailResponse MapToDetail(Patient p) => new()
    {
        PatientId                = p.PatientId,
        MedicalRecordNumber      = p.MedicalRecordNumber,
        FullName                 = $"{p.FirstName} {p.LastName}".Trim(),
        DateOfBirth              = p.DateOfBirth,
        Age                      = CalculateAge(p.DateOfBirth),
        Gender                   = p.Gender,
        PhoneNumber              = p.PhoneNumber,
        HasAllergies             = p.HasAllergies,
        IsActive                 = p.IsActive,
        RegisteredAt             = p.CreatedAt,
        MiddleName               = p.MiddleName,
        BloodType                = p.BloodType,
        NationalId               = p.NationalId,
        Email                    = p.Email,
        AlternatePhone           = p.AlternatePhone,
        AddressLine1             = p.AddressLine1,
        AddressLine2             = p.AddressLine2,
        City                     = p.City,
        State                    = p.State,
        PostalCode               = p.PostalCode,
        Country                  = p.Country,
        EmergencyContactName     = p.EmergencyContactName,
        EmergencyContactPhone    = p.EmergencyContactPhone,
        EmergencyContactRelation = p.EmergencyContactRelation,
        InsuranceProvider        = p.InsuranceProvider,
        InsurancePolicyNumber    = p.InsurancePolicyNumber,
        InsuranceGroupNumber     = p.InsuranceGroupNumber,
        HasChronicConditions     = p.HasChronicConditions
    };

    private static AllergyResponse MapToAllergyResponse(PatientAllergy a) => new()
    {
        AllergyId    = a.AllergyId,
        AllergyType  = a.AllergyType,
        AllergenName = a.AllergenName,
        Reaction     = a.Reaction,
        Severity     = a.Severity,
        RecordedAt   = a.RecordedAt
    };

    private static int CalculateAge(DateOnly dob)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age   = today.Year - dob.Year;
        if (dob > today.AddYears(-age)) age--;
        return age;
    }

    private async Task EnsurePatientExistsAsync(Guid patientId, CancellationToken ct)
    {
        var exists = await _db.Patients.AnyAsync(p => p.PatientId == patientId, ct);
        if (!exists) throw new NotFoundException(nameof(Patient), patientId);
    }
}
