using KayCare.Core.Constants;
using KayCare.Core.DTOs.Nursing;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KayCare.Infrastructure.Services;

public class MedicationAdministrationService : IMedicationAdministrationService
{
    private readonly AppDbContext        _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService       _audit;
    private readonly ILogger<MedicationAdministrationService> _logger;

    public MedicationAdministrationService(
        AppDbContext db,
        ICurrentUserService currentUser,
        IAuditService audit,
        ILogger<MedicationAdministrationService> logger)
    {
        _db          = db;
        _currentUser = currentUser;
        _audit       = audit;
        _logger      = logger;
    }

    public async Task<MAREntryResponse> RecordAsync(RecordAdministrationRequest req, CancellationToken ct = default)
    {
        var item = await _db.PrescriptionItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ItemId == req.PrescriptionItemId && i.TenantId == _db.Prescriptions
                .Where(p => p.PrescriptionId == req.PrescriptionId)
                .Select(p => p.TenantId)
                .FirstOrDefault(), ct)
            ?? throw new NotFoundException("PrescriptionItem", req.PrescriptionItemId);

        var admin = new MedicationAdministration
        {
            MedicationAdministrationId = Guid.NewGuid(),
            PrescriptionId             = req.PrescriptionId,
            PrescriptionItemId         = req.PrescriptionItemId,
            PatientId                  = req.PatientId,
            AdmissionId                = req.AdmissionId,
            AdministeredByUserId       = _currentUser.UserId,
            AdministeredAt             = DateTime.UtcNow,
            Status                     = req.Status,
            DoseGiven                  = req.DoseGiven,
            Route                      = req.Route,
            Notes                      = req.Notes,
        };
        _db.MedicationAdministrations.Add(admin);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.MedicationAdministrationRecord, nameof(MedicationAdministration), admin.MedicationAdministrationId, admin.PatientId,
            details: $"PrescriptionItemId={admin.PrescriptionItemId}; Status={admin.Status}; DoseGiven={admin.DoseGiven}", ct: ct);
        _logger.LogInformation("Medication administration {MedicationAdministrationId} recorded for patient {PatientId}: status {Status}",
            admin.MedicationAdministrationId, admin.PatientId, admin.Status);

        var nurse = await _db.Users.AsNoTracking().FirstAsync(u => u.UserId == admin.AdministeredByUserId, ct);
        return ToEntryResponse(admin, item, $"{nurse.FirstName} {nurse.LastName}");
    }

    public async Task<List<MARItemResponse>> GetMARForPatientAsync(Guid patientId, CancellationToken ct = default)
    {
        var prescriptions = await _db.Prescriptions
            .Include(p => p.Items)
            .AsNoTracking()
            .Where(p => p.PatientId == patientId)
            .ToListAsync(ct);

        return await BuildMAR(prescriptions, ct);
    }

    public async Task<List<MARItemResponse>> GetMARForAdmissionAsync(Guid admissionId, CancellationToken ct = default)
    {
        var admission = await _db.Admissions.AsNoTracking().FirstOrDefaultAsync(a => a.AdmissionId == admissionId, ct)
            ?? throw new NotFoundException("Admission", admissionId);

        var prescriptions = await _db.Prescriptions
            .Include(p => p.Items)
            .AsNoTracking()
            .Where(p => p.PatientId == admission.PatientId)
            .ToListAsync(ct);

        return await BuildMAR(prescriptions, ct);
    }

    private async Task<List<MARItemResponse>> BuildMAR(List<Prescription> prescriptions, CancellationToken ct)
    {
        var itemIds = prescriptions.SelectMany(p => p.Items).Select(i => i.ItemId).ToList();

        var admins = await _db.MedicationAdministrations
            .Include(a => a.AdministeredBy)
            .AsNoTracking()
            .Where(a => itemIds.Contains(a.PrescriptionItemId))
            .OrderByDescending(a => a.AdministeredAt)
            .ToListAsync(ct);

        var adminsByItem = admins.GroupBy(a => a.PrescriptionItemId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<MARItemResponse>();
        foreach (var rx in prescriptions)
        {
            foreach (var item in rx.Items)
            {
                var entries = adminsByItem.GetValueOrDefault(item.ItemId, [])
                    .Select(a => ToEntryResponse(a, item, $"{a.AdministeredBy.FirstName} {a.AdministeredBy.LastName}"))
                    .ToList();

                result.Add(new MARItemResponse(
                    item.ItemId, rx.PrescriptionId, rx.PatientId,
                    item.MedicationName, item.Strength,
                    item.Frequency, item.Instructions ?? string.Empty,
                    entries));
            }
        }
        return result;
    }

    private static MAREntryResponse ToEntryResponse(MedicationAdministration a, PrescriptionItem item, string nurseName) =>
        new(a.MedicationAdministrationId, a.PrescriptionId, a.PrescriptionItemId,
            item.MedicationName, item.Strength, item.Frequency,
            nurseName, a.AdministeredAt, a.Status, a.DoseGiven, a.Route, a.Notes);
}
