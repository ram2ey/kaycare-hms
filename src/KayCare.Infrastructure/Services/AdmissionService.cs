using KayCare.Core.Constants;
using KayCare.Core.DTOs.Inpatient;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KayCare.Infrastructure.Services;

public class AdmissionService : IAdmissionService
{
    private readonly AppDbContext        _db;
    private readonly ITenantContext      _tenantContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService       _audit;
    private readonly ILogger<AdmissionService> _logger;

    public AdmissionService(
        AppDbContext db,
        ITenantContext tenantContext,
        ICurrentUserService currentUser,
        IAuditService audit,
        ILogger<AdmissionService> logger)
    {
        _db            = db;
        _tenantContext = tenantContext;
        _currentUser   = currentUser;
        _audit         = audit;
        _logger        = logger;
    }

    public async Task<List<AdmissionSummaryResponse>> GetAllAsync(
        string? status = null, Guid? wardId = null, Guid? patientId = null,
        CancellationToken ct = default)
    {
        var query = _db.Admissions
            .Include(a => a.Patient)
            .Include(a => a.Bed)
            .Include(a => a.Ward)
            .Include(a => a.AdmittingDoctor)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status);
        if (wardId.HasValue)    query = query.Where(a => a.WardId    == wardId.Value);
        if (patientId.HasValue) query = query.Where(a => a.PatientId == patientId.Value);

        return await query
            .OrderByDescending(a => a.AdmissionDate)
            .Select(a => ToSummary(a))
            .ToListAsync(ct);
    }

    public async Task<AdmissionDetailResponse?> GetByIdAsync(Guid admissionId, CancellationToken ct = default)
    {
        var a = await LoadDetail(admissionId, ct);
        return a == null ? null : ToDetail(a);
    }

    public async Task<List<AdmissionSummaryResponse>> GetPatientHistoryAsync(Guid patientId, CancellationToken ct = default)
        => await GetAllAsync(patientId: patientId, ct: ct);

    public async Task<AdmissionDetailResponse> AdmitAsync(AdmitPatientRequest request, CancellationToken ct = default)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await _db.AcquireAdvisoryLockAsync(_tenantContext.TenantId, "AdmissionNumber", ct);

        // The AdmissionNumber lock above only serializes number generation — it does not stop
        // two concurrent admission requests from both seeing this bed as Available. Lock the
        // specific bed too, before loading and checking its status, so the status read below is
        // guaranteed to reflect any concurrently-committed change rather than a stale snapshot.
        await _db.AcquireAdvisoryLockAsync(_tenantContext.TenantId, $"Bed_{request.BedId}", ct);

        var bed = await _db.Beds.Include(b => b.Ward)
            .FirstOrDefaultAsync(b => b.BedId == request.BedId, ct)
            ?? throw new NotFoundException("Bed", request.BedId);

        if (bed.Status != BedStatus.Available)
            throw new AppException($"Bed {bed.BedNumber} is not available (status: {bed.Status}).", 400);

        _ = await _db.Patients.FirstOrDefaultAsync(p => p.PatientId == request.PatientId, ct)
            ?? throw new NotFoundException("Patient", request.PatientId);

        _ = await _db.Users.FirstOrDefaultAsync(u => u.UserId == request.AdmittingDoctorUserId, ct)
            ?? throw new NotFoundException("User", request.AdmittingDoctorUserId);

        var existing = await _db.Admissions
            .AnyAsync(a => a.PatientId == request.PatientId && a.Status == AdmissionStatus.Active, ct);
        if (existing)
            throw new AppException("Patient already has an active admission.", 409);

        var number = await GenerateAdmissionNumberAsync(ct);

        var admission = new Admission
        {
            AdmissionNumber        = number,
            PatientId              = request.PatientId,
            BedId                  = request.BedId,
            WardId                 = bed.WardId,
            AdmittingDoctorUserId  = request.AdmittingDoctorUserId,
            CreatedByUserId        = _currentUser.UserId,
            AdmissionDate          = DateTime.UtcNow,
            ExpectedDischargeDate  = request.ExpectedDischargeDate,
            Status                 = AdmissionStatus.Active,
            AdmissionReason        = request.AdmissionReason.Trim(),
            DiagnosisOnAdmission   = request.DiagnosisOnAdmission?.Trim(),
        };

        bed.Status = BedStatus.Occupied;

        _db.Admissions.Add(admission);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.AdmissionAdmit, nameof(Admission), admission.AdmissionId, admission.PatientId,
            details: $"AdmissionNumber={admission.AdmissionNumber}; BedId={admission.BedId}; WardId={admission.WardId}", ct: ct);
        _logger.LogInformation("Admission {AdmissionId} ({AdmissionNumber}) created for patient {PatientId} in bed {BedId}",
            admission.AdmissionId, admission.AdmissionNumber, admission.PatientId, admission.BedId);

        await transaction.CommitAsync(ct);

        return ToDetail((await LoadDetail(admission.AdmissionId, ct))!);
    }

    public async Task<AdmissionDetailResponse> DischargeAsync(
        Guid admissionId, DischargePatientRequest request, CancellationToken ct = default)
    {
        if (!DischargeType.All.Contains(request.DischargeType))
            throw new AppException($"Invalid discharge type '{request.DischargeType}'.", 400);

        var admission = await _db.Admissions
            .Include(a => a.Bed)
            .FirstOrDefaultAsync(a => a.AdmissionId == admissionId, ct)
            ?? throw new NotFoundException("Admission", admissionId);

        if (admission.Status != AdmissionStatus.Active)
            throw new AppException("Only active admissions can be discharged.", 400);

        admission.Status              = AdmissionStatus.Discharged;
        admission.ActualDischargeDate = DateTime.UtcNow;
        admission.DischargeType       = request.DischargeType;
        admission.DischargeNotes      = request.DischargeNotes?.Trim();

        admission.Bed.Status = BedStatus.Available;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.AdmissionDischarge, nameof(Admission), admissionId, admission.PatientId,
            details: $"DischargeType={admission.DischargeType}", ct: ct);
        _logger.LogInformation("Admission {AdmissionId} discharged, type {DischargeType}", admissionId, admission.DischargeType);

        return ToDetail((await LoadDetail(admissionId, ct))!);
    }

    public async Task<AdmissionDetailResponse> TransferAsync(
        Guid admissionId, TransferPatientRequest request, CancellationToken ct = default)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var admission = await _db.Admissions
            .Include(a => a.Bed)
            .FirstOrDefaultAsync(a => a.AdmissionId == admissionId, ct)
            ?? throw new NotFoundException("Admission", admissionId);

        if (admission.Status != AdmissionStatus.Active)
            throw new AppException("Only active admissions can be transferred.", 400);

        if (admission.BedId == request.NewBedId)
            throw new AppException("Patient is already in the requested bed.", 400);

        // Lock both the source and destination bed, in a consistent order (sorted by GUID) so
        // two concurrent transfers that cross each other's source/destination beds can't
        // deadlock. The destination bed is (re)loaded after both locks are held, so its status
        // check below reflects any concurrently-committed change rather than a stale snapshot.
        var (firstBedId, secondBedId) = admission.BedId.CompareTo(request.NewBedId) < 0
            ? (admission.BedId, request.NewBedId)
            : (request.NewBedId, admission.BedId);
        await _db.AcquireAdvisoryLockAsync(_tenantContext.TenantId, $"Bed_{firstBedId}", ct);
        await _db.AcquireAdvisoryLockAsync(_tenantContext.TenantId, $"Bed_{secondBedId}", ct);

        var newBed = await _db.Beds.Include(b => b.Ward)
            .FirstOrDefaultAsync(b => b.BedId == request.NewBedId, ct)
            ?? throw new NotFoundException("Bed", request.NewBedId);

        if (newBed.Status != BedStatus.Available)
            throw new AppException($"Bed {newBed.BedNumber} is not available (status: {newBed.Status}).", 400);

        var transfer = new AdmissionTransfer
        {
            TenantId           = _tenantContext.TenantId,
            AdmissionId        = admissionId,
            FromBedId          = admission.BedId,
            FromWardId         = admission.WardId,
            ToBedId            = request.NewBedId,
            ToWardId           = newBed.WardId,
            TransferredAt      = DateTime.UtcNow,
            Reason             = request.Reason?.Trim(),
            TransferredByUserId = _currentUser.UserId,
        };

        admission.Bed.Status = BedStatus.Available;
        newBed.Status        = BedStatus.Occupied;

        admission.BedId  = request.NewBedId;
        admission.WardId = newBed.WardId;

        _db.AdmissionTransfers.Add(transfer);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.AdmissionTransfer, nameof(Admission), admissionId, admission.PatientId,
            details: $"FromBedId={transfer.FromBedId}; ToBedId={transfer.ToBedId}; Reason={transfer.Reason}", ct: ct);
        _logger.LogInformation("Admission {AdmissionId} transferred from bed {FromBedId} to bed {ToBedId}",
            admissionId, transfer.FromBedId, transfer.ToBedId);

        await transaction.CommitAsync(ct);

        return ToDetail((await LoadDetail(admissionId, ct))!);
    }

    public async Task<DischargeSummaryResponse> GetDischargeSummaryAsync(Guid admissionId, CancellationToken ct = default)
    {
        var a = await _db.Admissions
            .Include(a => a.Patient)
            .Include(a => a.Bed)
            .Include(a => a.Ward)
            .Include(a => a.AdmittingDoctor)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AdmissionId == admissionId, ct)
            ?? throw new NotFoundException("Admission", admissionId);

        return ToDischargeSummary(a);
    }

    public async Task<DischargeSummaryResponse> UpdateDischargeSummaryAsync(
        Guid admissionId, UpdateDischargeSummaryRequest request, CancellationToken ct = default)
    {
        if (request.DischargeCondition != null &&
            !DischargeCondition.All.Contains(request.DischargeCondition))
            throw new AppException($"Invalid discharge condition '{request.DischargeCondition}'.", 400);

        var a = await _db.Admissions
            .Include(a => a.Patient)
            .Include(a => a.Bed)
            .Include(a => a.Ward)
            .Include(a => a.AdmittingDoctor)
            .FirstOrDefaultAsync(a => a.AdmissionId == admissionId, ct)
            ?? throw new NotFoundException("Admission", admissionId);

        a.FinalDiagnosis          = request.FinalDiagnosis?.Trim();
        a.TreatmentSummary        = request.TreatmentSummary?.Trim();
        a.ProceduresPerformed     = request.ProceduresPerformed?.Trim();
        a.DischargeMedications    = request.DischargeMedications?.Trim();
        a.DischargeCondition      = request.DischargeCondition?.Trim();
        a.FollowUpInstructions    = request.FollowUpInstructions?.Trim();
        a.AttendingPhysicianNotes = request.AttendingPhysicianNotes?.Trim();

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.AdmissionUpdateDischargeSummary, nameof(Admission), admissionId, a.PatientId, ct: ct);
        _logger.LogInformation("Discharge summary updated for admission {AdmissionId}", admissionId);

        return ToDischargeSummary(a);
    }

    private static DischargeSummaryResponse ToDischargeSummary(Admission a)
    {
        var los = a.ActualDischargeDate.HasValue
            ? (int)(a.ActualDischargeDate.Value - a.AdmissionDate).TotalDays
            : (int?)(null);
        return new DischargeSummaryResponse
        {
            AdmissionId            = a.AdmissionId,
            AdmissionNumber        = a.AdmissionNumber,
            PatientId              = a.PatientId,
            PatientName            = $"{a.Patient.FirstName} {a.Patient.LastName}",
            PatientMrn             = a.Patient.MedicalRecordNumber,
            PatientDateOfBirth     = a.Patient.DateOfBirth.ToString("dd-MMM-yyyy"),
            PatientGender          = a.Patient.Gender,
            PatientPhone           = a.Patient.PhoneNumber,
            WardName               = a.Ward.Name,
            WardType               = a.Ward.WardType,
            BedNumber              = a.Bed.BedNumber,
            AdmittingDoctorName    = $"Dr. {a.AdmittingDoctor.FirstName} {a.AdmittingDoctor.LastName}",
            AdmissionDate          = a.AdmissionDate,
            ActualDischargeDate    = a.ActualDischargeDate,
            LengthOfStayDays       = los,
            Status                 = a.Status,
            AdmissionReason        = a.AdmissionReason,
            DiagnosisOnAdmission   = a.DiagnosisOnAdmission,
            DischargeType          = a.DischargeType,
            DischargeNotes         = a.DischargeNotes,
            FinalDiagnosis         = a.FinalDiagnosis,
            TreatmentSummary       = a.TreatmentSummary,
            ProceduresPerformed    = a.ProceduresPerformed,
            DischargeMedications   = a.DischargeMedications,
            DischargeCondition     = a.DischargeCondition,
            FollowUpInstructions   = a.FollowUpInstructions,
            AttendingPhysicianNotes = a.AttendingPhysicianNotes,
            UpdatedAt              = a.UpdatedAt,
        };
    }

    private async Task<Admission?> LoadDetail(Guid admissionId, CancellationToken ct)
        => await _db.Admissions
            .Include(a => a.Patient)
            .Include(a => a.Bed)
            .Include(a => a.Ward)
            .Include(a => a.AdmittingDoctor)
            .Include(a => a.CreatedBy)
            .Include(a => a.Transfers)
                .ThenInclude(t => t.FromBed)
            .Include(a => a.Transfers)
                .ThenInclude(t => t.FromWard)
            .Include(a => a.Transfers)
                .ThenInclude(t => t.ToBed)
            .Include(a => a.Transfers)
                .ThenInclude(t => t.ToWard)
            .Include(a => a.Transfers)
                .ThenInclude(t => t.TransferredBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AdmissionId == admissionId, ct);

    private async Task<string> GenerateAdmissionNumberAsync(CancellationToken ct)
    {
        var year   = DateTime.UtcNow.Year;
        var prefix = $"ADM-{year}-";
        var last = await _db.Admissions
            .Where(a => a.AdmissionNumber.StartsWith(prefix))
            .OrderByDescending(a => a.AdmissionNumber)
            .Select(a => a.AdmissionNumber)
            .FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var lastNum))
            seq = lastNum + 1;
        return $"{prefix}{seq:D5}";
    }

    private static AdmissionSummaryResponse ToSummary(Admission a) => new()
    {
        AdmissionId           = a.AdmissionId,
        AdmissionNumber       = a.AdmissionNumber,
        PatientId             = a.PatientId,
        PatientName           = $"{a.Patient.FirstName} {a.Patient.LastName}",
        PatientMrn            = a.Patient.MedicalRecordNumber,
        WardName              = a.Ward.Name,
        BedNumber             = a.Bed.BedNumber,
        AdmittingDoctorName   = $"Dr. {a.AdmittingDoctor.FirstName} {a.AdmittingDoctor.LastName}",
        AdmissionDate         = a.AdmissionDate,
        ExpectedDischargeDate = a.ExpectedDischargeDate,
        Status                = a.Status,
        AdmissionReason       = a.AdmissionReason,
    };

    private static AdmissionDetailResponse ToDetail(Admission a) => new()
    {
        AdmissionId           = a.AdmissionId,
        AdmissionNumber       = a.AdmissionNumber,
        PatientId             = a.PatientId,
        PatientName           = $"{a.Patient.FirstName} {a.Patient.LastName}",
        PatientMrn            = a.Patient.MedicalRecordNumber,
        WardName              = a.Ward.Name,
        WardType              = a.Ward.WardType,
        BedId                 = a.BedId,
        WardId                = a.WardId,
        BedNumber             = a.Bed.BedNumber,
        AdmittingDoctorName   = $"Dr. {a.AdmittingDoctor.FirstName} {a.AdmittingDoctor.LastName}",
        AdmissionDate         = a.AdmissionDate,
        ExpectedDischargeDate = a.ExpectedDischargeDate,
        ActualDischargeDate   = a.ActualDischargeDate,
        Status                = a.Status,
        AdmissionReason       = a.AdmissionReason,
        DiagnosisOnAdmission  = a.DiagnosisOnAdmission,
        DischargeNotes        = a.DischargeNotes,
        DischargeType         = a.DischargeType,
        CreatedByName         = $"{a.CreatedBy.FirstName} {a.CreatedBy.LastName}",
        CreatedAt             = a.CreatedAt,
        UpdatedAt             = a.UpdatedAt,
        Transfers             = a.Transfers
            .OrderBy(t => t.TransferredAt)
            .Select(t => new AdmissionTransferResponse
            {
                AdmissionTransferId = t.AdmissionTransferId,
                FromBedNumber       = t.FromBed.BedNumber,
                FromWardName        = t.FromWard.Name,
                ToBedNumber         = t.ToBed.BedNumber,
                ToWardName          = t.ToWard.Name,
                TransferredAt       = t.TransferredAt,
                Reason              = t.Reason,
                TransferredByName   = $"{t.TransferredBy.FirstName} {t.TransferredBy.LastName}",
            }).ToList(),
    };
}
