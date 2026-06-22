using KayCare.Core.Constants;
using KayCare.Core.DTOs.Radiology;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KayCare.Infrastructure.Services;

public class RadiologyOrderService : IRadiologyOrderService
{
    private readonly AppDbContext        _db;
    private readonly ITenantContext      _tenantContext;
    private readonly ICurrentUserService _currentUser;

    public RadiologyOrderService(AppDbContext db, ITenantContext tenantContext, ICurrentUserService currentUser)
    {
        _db            = db;
        _tenantContext = tenantContext;
        _currentUser   = currentUser;
    }

    public async Task<IReadOnlyList<ImagingProcedureResponse>> GetProcedureCatalogAsync(CancellationToken ct)
    {
        await ValidateAccessAsync(ct);
        var procedures = await _db.ImagingProcedures
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Modality)
            .ThenBy(p => p.ProcedureName)
            .ToListAsync(ct);

        return procedures.Select(MapProcedure).ToList();
    }

    public async Task<IReadOnlyList<ImagingProcedureResponse>> GetFullProcedureCatalogAsync(CancellationToken ct)
    {
        var procedures = await _db.ImagingProcedures
            .AsNoTracking()
            .OrderBy(p => p.Modality)
            .ThenBy(p => p.ProcedureName)
            .ToListAsync(ct);

        return procedures.Select(MapProcedure).ToList();
    }

    public async Task<ImagingProcedureResponse> CreateProcedureAsync(SaveImagingProcedureRequest request, CancellationToken ct)
    {
        var procedure = new ImagingProcedure
        {
            ImagingProcedureId = Guid.NewGuid(),
            ProcedureCode      = request.ProcedureCode.Trim().ToUpperInvariant(),
            ProcedureName      = request.ProcedureName.Trim(),
            Modality           = request.Modality.Trim(),
            BodyPart           = request.BodyPart.Trim(),
            Department         = request.Department.Trim(),
            TatHours           = request.TatHours,
            IsActive           = request.IsActive,
        };
        _db.ImagingProcedures.Add(procedure);
        await _db.SaveChangesAsync(ct);
        return MapProcedure(procedure);
    }

    public async Task<ImagingProcedureResponse> UpdateProcedureAsync(Guid id, SaveImagingProcedureRequest request, CancellationToken ct)
    {
        var procedure = await _db.ImagingProcedures.FindAsync(new object[] { id }, ct)
            ?? throw new NotFoundException("ImagingProcedure", id);

        procedure.ProcedureCode = request.ProcedureCode.Trim().ToUpperInvariant();
        procedure.ProcedureName = request.ProcedureName.Trim();
        procedure.Modality      = request.Modality.Trim();
        procedure.BodyPart      = request.BodyPart.Trim();
        procedure.Department    = request.Department.Trim();
        procedure.TatHours      = request.TatHours;
        procedure.IsActive      = request.IsActive;

        await _db.SaveChangesAsync(ct);
        return MapProcedure(procedure);
    }

    public async Task<ImagingProcedureResponse> ToggleProcedureActiveAsync(Guid id, CancellationToken ct)
    {
        var procedure = await _db.ImagingProcedures.FindAsync(new object[] { id }, ct)
            ?? throw new NotFoundException("ImagingProcedure", id);

        procedure.IsActive = !procedure.IsActive;
        await _db.SaveChangesAsync(ct);
        return MapProcedure(procedure);
    }

    private static readonly SemaphoreSlim _accessionSemaphore = new SemaphoreSlim(1, 1);

    public async Task<RadiologyOrderDetailResponse> PlaceOrderAsync(CreateRadiologyOrderRequest req, CancellationToken ct)
    {
        await ValidateAccessAsync(ct);
        if (req.ProcedureIds.Count == 0)
            throw new AppException("At least one procedure must be selected.");

        var patient = await _db.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PatientId == req.PatientId, ct)
            ?? throw new NotFoundException("Patient", req.PatientId);

        var procedures = await _db.ImagingProcedures
            .Where(p => req.ProcedureIds.Contains(p.ImagingProcedureId) && p.IsActive)
            .ToListAsync(ct);

        if (procedures.Count != req.ProcedureIds.Count)
            throw new AppException("One or more selected procedures were not found or are inactive.");

        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await _db.AcquireAdvisoryLockAsync(_tenantContext.TenantId, "RadiologyAccessionNumber", ct);

        await _accessionSemaphore.WaitAsync(ct);
        try
        {
            var order = new RadiologyOrder
            {
                RadiologyOrderId     = Guid.NewGuid(),
                PatientId            = req.PatientId,
                BillId               = req.BillId,
                OrderingDoctorUserId = _currentUser.UserId,
                Priority             = req.Priority ?? "Routine",
                Status               = RadiologyOrderStatus.Pending,
                ClinicalIndication   = req.ClinicalIndication?.Trim(),
                Notes                = req.Notes?.Trim(),
            };
            _db.RadiologyOrders.Add(order);
            await _db.SaveChangesAsync(ct);

            foreach (var proc in procedures)
            {
                var accession = await GenerateAccessionAsync(ct);
                var item = new RadiologyOrderItem
                {
                    RadiologyOrderItemId = Guid.NewGuid(),
                    RadiologyOrderId     = order.RadiologyOrderId,
                    TenantId             = _tenantContext.TenantId,
                    ImagingProcedureId   = proc.ImagingProcedureId,
                    ProcedureName        = proc.ProcedureName,
                    Modality             = proc.Modality,
                    BodyPart             = proc.BodyPart,
                    Department           = proc.Department,
                    TatHours             = proc.TatHours,
                    AccessionNumber      = accession,
                    Status               = RadiologyOrderItemStatus.Ordered,
                };
                _db.RadiologyOrderItems.Add(item);
            }

            order.Status = RadiologyOrderStatus.Scheduled;
            await _db.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

            return await LoadDetailAsync(order.RadiologyOrderId, ct);
        }
        finally
        {
            _accessionSemaphore.Release();
        }
    }

    public async Task<IReadOnlyList<RadiologyOrderResponse>> GetByPatientAsync(Guid patientId, CancellationToken ct)
    {
        await ValidateAccessAsync(ct);
        var orders = await _db.RadiologyOrders
            .Include(o => o.Patient)
            .Include(o => o.OrderingDoctor)
            .Include(o => o.Items)
            .AsNoTracking()
            .Where(o => o.PatientId == patientId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        return orders.Select(MapSummary).ToList();
    }

    public async Task<IReadOnlyList<RadiologyOrderResponse>> GetWorklistAsync(DateOnly date, string? status, CancellationToken ct)
    {
        await ValidateAccessAsync(ct);
        var from = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to   = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var query = _db.RadiologyOrders
            .Include(o => o.Patient)
            .Include(o => o.OrderingDoctor)
            .Include(o => o.Items)
            .AsNoTracking()
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
        return orders.Select(MapSummary).ToList();
    }

    public async Task<RadiologyOrderDetailResponse?> GetByIdAsync(Guid radiologyOrderId, CancellationToken ct)
    {
        await ValidateAccessAsync(ct);
        var order = await _db.RadiologyOrders
            .Include(o => o.Patient)
            .Include(o => o.OrderingDoctor)
            .Include(o => o.Bill)
            .Include(o => o.Items)
                .ThenInclude(i => i.ImagingProcedure)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.RadiologyOrderId == radiologyOrderId, ct);

        if (order == null) return null;

        var reportingDoctorIds = order.Items
            .Where(i => i.ReportingDoctorUserId.HasValue)
            .Select(i => i.ReportingDoctorUserId!.Value)
            .Distinct()
            .ToList();

        var doctors = reportingDoctorIds.Count > 0
            ? await _db.Users.AsNoTracking()
                .Where(u => reportingDoctorIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => $"{u.FirstName} {u.LastName}", ct)
            : new Dictionary<Guid, string>();

        var summary = MapSummary(order);
        return new RadiologyOrderDetailResponse
        {
            RadiologyOrderId     = summary.RadiologyOrderId,
            PatientId            = summary.PatientId,
            PatientName          = summary.PatientName,
            PatientMrn           = summary.PatientMrn,
            PatientGender        = summary.PatientGender,
            PatientDob           = summary.PatientDob,
            BillId               = summary.BillId,
            BillNumber           = summary.BillNumber,
            OrderingDoctorUserId = summary.OrderingDoctorUserId,
            OrderingDoctorName   = summary.OrderingDoctorName,
            Priority             = summary.Priority,
            Status               = summary.Status,
            ClinicalIndication   = summary.ClinicalIndication,
            Notes                = summary.Notes,
            OrderedAt            = summary.OrderedAt,
            IncompleteCount      = summary.IncompleteCount,
            ReportedCount        = summary.ReportedCount,
            SignedCount          = summary.SignedCount,
            ProcedureNames       = summary.ProcedureNames,
            Items                = order.Items.Select(i => MapItem(i, doctors)).ToList(),
        };
    }

    public async Task<RadiologyOrderItemResponse> MarkAcquiredAsync(Guid itemId, string? pacsUrl, CancellationToken ct)
    {
        await ValidateAccessAsync(ct);
        var item = await LoadItem(itemId, ct);

        if (item.Status != RadiologyOrderItemStatus.Ordered)
            throw new ConflictException("Item must be in Ordered status to mark as acquired.");

        item.Status     = RadiologyOrderItemStatus.Acquired;
        item.AcquiredAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(pacsUrl))
        {
            item.PacsViewerUrl = pacsUrl;
        }
        await _db.SaveChangesAsync(ct);
        await UpdateOrderStatusAsync(item.RadiologyOrderId, ct);
        return MapItem(item, null);
    }

    public async Task<RadiologyOrderItemResponse> EnterReportAsync(Guid itemId, RadiologyReportRequest req, CancellationToken ct)
    {
        await ValidateAccessAsync(ct);
        var item = await LoadItem(itemId, ct);

        if (item.Status == RadiologyOrderItemStatus.Signed)
            throw new ConflictException("Cannot update a signed report.");

        item.Findings               = req.Findings?.Trim();
        item.Impression             = req.Impression?.Trim();
        item.Recommendations        = req.Recommendations?.Trim();
        item.PacsStudyUid           = req.PacsStudyUid?.Trim();
        item.PacsViewerUrl          = req.PacsViewerUrl?.Trim();
        item.ReportingDoctorUserId  = _currentUser.UserId;
        item.Status                 = RadiologyOrderItemStatus.Reported;
        item.ReportedAt             = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await UpdateOrderStatusAsync(item.RadiologyOrderId, ct);
        return MapItem(item, null);
    }

    public async Task<RadiologyOrderItemResponse> SignItemAsync(Guid itemId, CancellationToken ct)
    {
        await ValidateAccessAsync(ct);
        var item = await LoadItem(itemId, ct);

        if (item.Status != RadiologyOrderItemStatus.Reported)
            throw new ConflictException("Only reported items can be signed.");

        item.Status         = RadiologyOrderItemStatus.Signed;
        item.SignedAt       = DateTime.UtcNow;
        item.SignedByUserId = _currentUser.UserId;

        await _db.SaveChangesAsync(ct);
        await UpdateOrderStatusAsync(item.RadiologyOrderId, ct);
        return MapItem(item, null);
    }

    private async Task UpdateOrderStatusAsync(Guid radiologyOrderId, CancellationToken ct)
    {
        var items = await _db.RadiologyOrderItems
            .Where(i => i.RadiologyOrderId == radiologyOrderId)
            .ToListAsync(ct);

        var order = await _db.RadiologyOrders
            .FirstOrDefaultAsync(o => o.RadiologyOrderId == radiologyOrderId, ct);
        if (order == null) return;

        if (items.All(i => i.Status == RadiologyOrderItemStatus.Signed))
            order.Status = RadiologyOrderStatus.Signed;
        else if (items.All(i => i.Status == RadiologyOrderItemStatus.Reported || i.Status == RadiologyOrderItemStatus.Signed))
            order.Status = RadiologyOrderStatus.Completed;
        else if (items.Any(i => i.Status == RadiologyOrderItemStatus.Reported || i.Status == RadiologyOrderItemStatus.Signed || i.Status == RadiologyOrderItemStatus.Acquired))
            order.Status = RadiologyOrderStatus.InProgress;
        else
            order.Status = RadiologyOrderStatus.Scheduled;

        await _db.SaveChangesAsync(ct);
    }

    private async Task<RadiologyOrderDetailResponse> LoadDetailAsync(Guid radiologyOrderId, CancellationToken ct)
    {
        var result = await GetByIdAsync(radiologyOrderId, ct);
        return result ?? throw new NotFoundException("RadiologyOrder", radiologyOrderId);
    }

    private async Task<RadiologyOrderItem> LoadItem(Guid itemId, CancellationToken ct)
        => await _db.RadiologyOrderItems.FirstOrDefaultAsync(i => i.RadiologyOrderItemId == itemId, ct)
            ?? throw new NotFoundException("RadiologyOrderItem", itemId);

    private async Task<string> GenerateAccessionAsync(CancellationToken ct)
    {
        var year   = DateTime.UtcNow.Year;
        var prefix = $"RAD-{year}-";
        var last = await _db.RadiologyOrderItems
            .Where(i => i.AccessionNumber != null && i.AccessionNumber.StartsWith(prefix))
            .OrderByDescending(i => i.AccessionNumber)
            .Select(i => i.AccessionNumber!)
            .FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var lastNum))
            seq = lastNum + 1;
        // Also incorporate any added items in the ChangeTracker to prevent collision in multi-item orders
        var localCount = _db.ChangeTracker.Entries<RadiologyOrderItem>()
            .Count(e => e.State == EntityState.Added 
                && e.Entity.TenantId == _tenantContext.TenantId
                && e.Entity.AccessionNumber != null 
                && e.Entity.AccessionNumber.StartsWith(prefix));
        return $"{prefix}{(seq + localCount):D5}";
    }

    private static RadiologyOrderResponse MapSummary(RadiologyOrder o)
    {
        var incomplete = o.Items.Count(i => i.Status == RadiologyOrderItemStatus.Ordered || i.Status == RadiologyOrderItemStatus.Acquired);
        var reported   = o.Items.Count(i => i.Status == RadiologyOrderItemStatus.Reported);
        var signed     = o.Items.Count(i => i.Status == RadiologyOrderItemStatus.Signed);

        return new RadiologyOrderResponse
        {
            RadiologyOrderId     = o.RadiologyOrderId,
            PatientId            = o.PatientId,
            PatientName          = $"{o.Patient.FirstName} {o.Patient.LastName}",
            PatientMrn           = o.Patient.MedicalRecordNumber,
            PatientGender        = o.Patient.Gender,
            PatientDob           = o.Patient.DateOfBirth,
            BillId               = o.BillId,
            BillNumber           = o.Bill?.BillNumber,
            OrderingDoctorUserId = o.OrderingDoctorUserId,
            OrderingDoctorName   = $"{o.OrderingDoctor.FirstName} {o.OrderingDoctor.LastName}",
            Priority             = o.Priority,
            Status               = o.Status,
            ClinicalIndication   = o.ClinicalIndication,
            Notes                = o.Notes,
            OrderedAt            = o.CreatedAt,
            IncompleteCount      = incomplete,
            ReportedCount        = reported,
            SignedCount          = signed,
            ProcedureNames       = o.Items.Select(i => i.ProcedureName).ToList(),
        };
    }

    private static RadiologyOrderItemResponse MapItem(RadiologyOrderItem i, Dictionary<Guid, string>? doctors) => new()
    {
        RadiologyOrderItemId  = i.RadiologyOrderItemId,
        ImagingProcedureId    = i.ImagingProcedureId,
        ProcedureName         = i.ProcedureName,
        Modality              = i.Modality,
        BodyPart              = i.BodyPart,
        Department            = i.Department,
        TatHours              = i.TatHours,
        AccessionNumber       = i.AccessionNumber,
        Status                = i.Status,
        AcquiredAt            = i.AcquiredAt,
        ReportedAt            = i.ReportedAt,
        SignedAt              = i.SignedAt,
        Findings              = i.Findings,
        Impression            = i.Impression,
        Recommendations       = i.Recommendations,
        ReportingDoctorName   = i.ReportingDoctorUserId.HasValue && doctors != null
            ? doctors.GetValueOrDefault(i.ReportingDoctorUserId.Value)
            : null,
        PacsViewerUrl         = i.PacsViewerUrl,
        IsTatExceeded         = i.AcquiredAt.HasValue
            && i.Status != RadiologyOrderItemStatus.Signed
            && DateTime.UtcNow > i.AcquiredAt.Value.AddHours(i.TatHours),
    };

    private static ImagingProcedureResponse MapProcedure(ImagingProcedure p) => new()
    {
        ImagingProcedureId = p.ImagingProcedureId,
        ProcedureCode      = p.ProcedureCode,
        ProcedureName      = p.ProcedureName,
        Modality           = p.Modality,
        BodyPart           = p.BodyPart,
        Department         = p.Department,
        TatHours           = p.TatHours,
        IsActive           = p.IsActive,
    };

    public async Task<RadiologyStatsResponse> GetStatsAsync(CancellationToken ct)
    {
        await ValidateAccessAsync(ct);
        var scheduled = await _db.RadiologyOrders
            .CountAsync(o => o.Status == RadiologyOrderStatus.Scheduled, ct);

        var acquired = await _db.RadiologyOrderItems
            .CountAsync(i => i.Status == RadiologyOrderItemStatus.Acquired, ct);

        var reported = await _db.RadiologyOrderItems
            .CountAsync(i => i.Status == RadiologyOrderItemStatus.Reported, ct);

        return new RadiologyStatsResponse
        {
            ScheduledCount = scheduled,
            AcquiredCount = acquired,
            ReportedCount = reported
        };
    }

    private async Task ValidateAccessAsync(CancellationToken ct)
    {
        var userRole = _currentUser.Role;
        if (userRole != Roles.SuperAdmin && userRole != Roles.Admin)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == _currentUser.UserId, ct);
            if (user != null && user.Department == "Laboratory")
            {
                throw new ForbiddenException("Laboratory staff cannot access Radiology data.");
            }
        }
    }
}
