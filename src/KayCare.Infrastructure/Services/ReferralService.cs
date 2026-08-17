using KayCare.Core.Constants;
using KayCare.Core.DTOs.Referrals;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KayCare.Infrastructure.Services;

public class ReferralService : IReferralService
{
    private readonly AppDbContext        _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService       _audit;
    private readonly ILogger<ReferralService> _logger;

    public ReferralService(AppDbContext db, ICurrentUserService currentUser, IAuditService audit, ILogger<ReferralService> logger)
    {
        _db          = db;
        _currentUser = currentUser;
        _audit       = audit;
        _logger      = logger;
    }

    public async Task<IReadOnlyList<ReferralSummaryResponse>> GetAllAsync(string? status, CancellationToken ct)
    {
        var q = _db.Referrals
            .Include(r => r.Patient)
            .Include(r => r.ReferringDoctor)
            .Include(r => r.ReferredToDoctor)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(r => r.Status == status);

        var list = await q.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
        return list.Select(ToSummary).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<ReferralSummaryResponse>> GetForPatientAsync(Guid patientId, CancellationToken ct)
    {
        var list = await _db.Referrals
            .Include(r => r.Patient)
            .Include(r => r.ReferringDoctor)
            .Include(r => r.ReferredToDoctor)
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        return list.Select(ToSummary).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<ReferralSummaryResponse>> GetIncomingAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var list = await _db.Referrals
            .Include(r => r.Patient)
            .Include(r => r.ReferringDoctor)
            .Include(r => r.ReferredToDoctor)
            .Where(r => r.ReferredToDoctorUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        return list.Select(ToSummary).ToList().AsReadOnly();
    }

    public async Task<ReferralDetailResponse> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var r = await LoadOrThrow(id, ct);
        return ToDetail(r);
    }

    public async Task<ReferralDetailResponse> CreateAsync(CreateReferralRequest req, CancellationToken ct)
    {
        // Previously used a CountAsync(...year) + 1 approach with no lookback and no locking —
        // collision-prone under concurrency (two simultaneous referrals in the same tenant/year
        // could compute the same count and the same number) unlike every other sequence-number
        // generator in this codebase, all of which use a lookback-highest-number query wrapped in
        // an advisory lock. Brought in line with that pattern.
        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await _db.AcquireAdvisoryLockAsync(_currentUser.TenantId, "ReferralNumber", ct);

        var number = await _db.GenerateSequenceNumberAsync(
            _db.Referrals.Select(r => r.ReferralNumber), $"REF-{DateTime.UtcNow.Year}-", ct);

        var referral = new Referral
        {
            ReferralNumber         = number,
            PatientId              = req.PatientId,
            ConsultationId         = req.ConsultationId,
            ReferringDoctorUserId  = _currentUser.UserId,
            ReferredToDoctorUserId = req.ReferredToDoctorUserId,
            ReferredToDepartment   = req.ReferredToDepartment,
            ReferralType           = req.ReferralType,
            ExternalFacility       = req.ExternalFacility,
            Urgency                = req.Urgency,
            Reason                 = req.Reason,
            ClinicalNotes          = req.ClinicalNotes,
            Status                 = "Draft",
        };

        _db.Referrals.Add(referral);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.ReferralCreate, nameof(Referral), referral.ReferralId, referral.PatientId,
            details: $"ReferralNumber={referral.ReferralNumber}", ct: ct);
        _logger.LogInformation("Referral {ReferralId} ({ReferralNumber}) created for patient {PatientId}",
            referral.ReferralId, referral.ReferralNumber, referral.PatientId);

        await transaction.CommitAsync(ct);

        return ToDetail(await LoadOrThrow(referral.ReferralId, ct));
    }

    public async Task<ReferralDetailResponse> UpdateAsync(Guid id, UpdateReferralRequest req, CancellationToken ct)
    {
        var referral = await LoadOrThrow(id, ct);

        if (referral.Status != "Draft")
            throw new AppException("Only Draft referrals can be edited.", 400);

        referral.ReferredToDoctorUserId = req.ReferredToDoctorUserId;
        referral.ReferredToDepartment   = req.ReferredToDepartment;
        referral.ReferralType           = req.ReferralType;
        referral.ExternalFacility       = req.ExternalFacility;
        referral.Urgency                = req.Urgency;
        referral.Reason                 = req.Reason;
        referral.ClinicalNotes          = req.ClinicalNotes;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.ReferralUpdate, nameof(Referral), id, referral.PatientId, ct: ct);
        _logger.LogInformation("Referral {ReferralId} updated", id);

        return ToDetail(await LoadOrThrow(id, ct));
    }

    public async Task<ReferralDetailResponse> SendAsync(Guid id, CancellationToken ct)
    {
        var referral = await LoadOrThrow(id, ct);

        if (referral.Status != "Draft")
            throw new AppException("Only Draft referrals can be sent.", 400);

        referral.Status = "Sent";
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.ReferralSend, nameof(Referral), id, referral.PatientId, ct: ct);
        _logger.LogInformation("Referral {ReferralId} sent", id);

        return ToDetail(await LoadOrThrow(id, ct));
    }

    public async Task<ReferralDetailResponse> RespondAsync(Guid id, RespondToReferralRequest req, CancellationToken ct)
    {
        var referral = await LoadOrThrow(id, ct);

        if (referral.Status != "Sent" && referral.Status != "Accepted")
            throw new AppException("Referral is not in a state that allows a response.", 400);

        referral.Status = req.Action switch
        {
            "Accept"   => "Accepted",
            "Complete" => "Completed",
            "Decline"  => "Declined",
            _          => throw new AppException("Invalid action. Use Accept, Complete, or Decline.", 400),
        };

        referral.ResponseNotes = req.ResponseNotes;
        await _db.SaveChangesAsync(ct);

        string action;
        if (req.Action == "Accept")        action = AuditActions.ReferralAccept;
        else if (req.Action == "Complete") action = AuditActions.ReferralComplete;
        else                                action = AuditActions.ReferralDecline; // only remaining valid value per validation above

        await _audit.LogAsync(action, nameof(Referral), id, referral.PatientId,
            details: $"Action={req.Action}; Status={referral.Status}", ct: ct);

        if (req.Action == "Decline")
            _logger.LogWarning("Referral {ReferralId} declined", id);
        else
            _logger.LogInformation("Referral {ReferralId} response recorded: {Action} -> {Status}", id, req.Action, referral.Status);

        return ToDetail(await LoadOrThrow(id, ct));
    }

    public async Task<ReferralDetailResponse> CancelAsync(Guid id, CancellationToken ct)
    {
        var referral = await LoadOrThrow(id, ct);

        if (referral.Status is "Completed" or "Declined" or "Cancelled")
            throw new AppException("Referral cannot be cancelled in its current state.", 400);

        referral.Status = "Cancelled";
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.ReferralCancel, nameof(Referral), id, referral.PatientId, ct: ct);
        _logger.LogWarning("Referral {ReferralId} cancelled", id);

        return ToDetail(await LoadOrThrow(id, ct));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Referral> LoadOrThrow(Guid id, CancellationToken ct)
    {
        var r = await _db.Referrals
            .Include(r => r.Patient)
            .Include(r => r.ReferringDoctor)
            .Include(r => r.ReferredToDoctor)
            .FirstOrDefaultAsync(r => r.ReferralId == id, ct);

        if (r is null) throw new NotFoundException("Referral", id);
        return r;
    }

    private static ReferralSummaryResponse ToSummary(Referral r) => new(
        r.ReferralId,
        r.ReferralNumber,
        $"{r.Patient.FirstName} {r.Patient.LastName}",
        r.Patient.MedicalRecordNumber,
        $"{r.ReferringDoctor.FirstName} {r.ReferringDoctor.LastName}",
        r.ReferredToDoctor is null ? null : $"{r.ReferredToDoctor.FirstName} {r.ReferredToDoctor.LastName}",
        r.ReferredToDepartment,
        r.ReferralType,
        r.ExternalFacility,
        r.Urgency,
        r.Status,
        r.CreatedAt
    );

    private static ReferralDetailResponse ToDetail(Referral r) => new(
        r.ReferralId,
        r.ReferralNumber,
        r.PatientId,
        $"{r.Patient.FirstName} {r.Patient.LastName}",
        r.Patient.MedicalRecordNumber,
        r.ReferringDoctorUserId,
        $"{r.ReferringDoctor.FirstName} {r.ReferringDoctor.LastName}",
        r.ReferredToDoctorUserId,
        r.ReferredToDoctor is null ? null : $"{r.ReferredToDoctor.FirstName} {r.ReferredToDoctor.LastName}",
        r.ReferredToDepartment,
        r.ReferralType,
        r.ExternalFacility,
        r.Urgency,
        r.Reason,
        r.ClinicalNotes,
        r.Status,
        r.ResponseNotes,
        r.ConsultationId,
        r.CreatedAt,
        r.UpdatedAt
    );
}
