using KayCare.Core.Constants;
using KayCare.Core.DTOs.LabOrders;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Services;

public class LabOrderService : ILabOrderService
{
    private readonly AppDbContext        _db;
    private readonly ICurrentUserService _currentUser;

    public LabOrderService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    // ── Test catalog ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<LabTestCatalogResponse>> GetTestCatalogAsync(CancellationToken ct)
    {
        var tests = await _db.LabTestCatalog
            .Where(t => t.IsActive)
            .OrderBy(t => t.Department)
            .ThenBy(t => t.TestName)
            .AsNoTracking()
            .ToListAsync(ct);

        return tests.Select(ToCatalogResponse).ToList().AsReadOnly();
    }

    // ── Place order ───────────────────────────────────────────────────────────

    public async Task<LabOrderDetailResponse> PlaceOrderAsync(CreateLabOrderRequest req, CancellationToken ct)
    {
        if (req.TestIds.Count == 0)
            throw new AppException("At least one test must be selected.", 400);

        var doctorId = _currentUser.UserId;

        // Load requested catalog items
        var catalogItems = await _db.LabTestCatalog
            .Where(t => req.TestIds.Contains(t.LabTestCatalogId) && t.IsActive)
            .AsNoTracking()
            .ToListAsync(ct);

        if (catalogItems.Count != req.TestIds.Distinct().Count())
            throw new AppException("One or more selected tests are invalid.", 400);

        var order = new LabOrder
        {
            PatientId            = req.PatientId,
            ConsultationId       = req.ConsultationId,
            BillId               = req.BillId,
            OrderingDoctorUserId = doctorId,
            Organisation         = req.Organisation,
            Status               = LabOrderStatus.Pending,
            Notes                = req.Notes,
        };

        _db.LabOrders.Add(order);
        await _db.SaveChangesAsync(ct); // get LabOrderId

        // Create items — TenantId set manually (no TenantEntity base)
        var tenantId = _currentUser.TenantId;
        foreach (var catalog in catalogItems)
        {
            _db.LabOrderItems.Add(new LabOrderItem
            {
                LabOrderId       = order.LabOrderId,
                TenantId         = tenantId,
                LabTestCatalogId = catalog.LabTestCatalogId,
                TestName         = catalog.TestName,
                Department       = catalog.Department,
                InstrumentType   = catalog.InstrumentType,
                IsManualEntry    = catalog.IsManualEntry,
                TatHours         = catalog.TatHours,
                Status           = LabOrderItemStatus.Ordered,
            });
        }

        await _db.SaveChangesAsync(ct);

        var result = await GetByIdAsync(order.LabOrderId, ct);
        return result!;
    }

    // ── Waiting list ──────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<LabOrderResponse>> GetWaitingListAsync(
        DateOnly date, string? status, string? department, CancellationToken ct)
    {
        var startUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc   = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var query = _db.LabOrders
            .Include(o => o.Patient)
            .Include(o => o.OrderingDoctor)
            .Include(o => o.Bill)
            .Include(o => o.Items)
            .Where(o => o.CreatedAt >= startUtc && o.CreatedAt <= endUtc);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);

        if (!string.IsNullOrEmpty(department))
            query = query.Where(o => o.Items.Any(i => i.Department == department));

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        return orders.Select(ToResponse).ToList().AsReadOnly();
    }

    // ── By patient ────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<LabOrderResponse>> GetByPatientAsync(Guid patientId, CancellationToken ct)
    {
        var orders = await _db.LabOrders
            .Include(o => o.Patient)
            .Include(o => o.OrderingDoctor)
            .Include(o => o.Bill)
            .Include(o => o.Items)
            .Where(o => o.PatientId == patientId)
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        return orders.Select(ToResponse).ToList().AsReadOnly();
    }

    // ── By ID ─────────────────────────────────────────────────────────────────

    public async Task<LabOrderDetailResponse?> GetByIdAsync(Guid labOrderId, CancellationToken ct)
    {
        var order = await _db.LabOrders
            .Include(o => o.Patient)
            .Include(o => o.OrderingDoctor)
            .Include(o => o.Bill)
            .Include(o => o.Items)
                .ThenInclude(i => i.LabTestCatalog)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.LabOrderId == labOrderId, ct);

        return order == null ? null : ToDetailResponse(order);
    }

    // ── Receive sample ────────────────────────────────────────────────────────

    public async Task<LabOrderItemResponse> ReceiveSampleAsync(Guid labOrderItemId, CancellationToken ct)
    {
        var item = await _db.LabOrderItems
            .Include(i => i.LabOrder)
            .FirstOrDefaultAsync(i => i.LabOrderItemId == labOrderItemId, ct)
            ?? throw new NotFoundException("LabOrderItem", labOrderItemId);

        if (item.Status != LabOrderItemStatus.Ordered)
            throw new AppException($"Item is already in status '{item.Status}'.", 409);

        item.AccessionNumber  = await GenerateAccessionNumberAsync(ct);
        item.Status           = LabOrderItemStatus.SampleReceived;
        item.SampleReceivedAt = DateTime.UtcNow;

        // Advance order to Active on first received sample
        if (item.LabOrder.Status == LabOrderStatus.Pending)
            item.LabOrder.Status = LabOrderStatus.Active;

        await _db.SaveChangesAsync(ct);

        return ToItemResponse(item);
    }

    // ── Manual result entry ───────────────────────────────────────────────────

    public async Task<LabOrderItemResponse> EnterManualResultAsync(
        Guid labOrderItemId, ManualResultRequest req, CancellationToken ct)
    {
        var item = await _db.LabOrderItems
            .Include(i => i.LabOrder)
                .ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(i => i.LabOrderItemId == labOrderItemId, ct)
            ?? throw new NotFoundException("LabOrderItem", labOrderItemId);

        if (!item.IsManualEntry)
            throw new AppException("This test is not a manual-entry test.", 400);

        if (item.Status == LabOrderItemStatus.Signed)
            throw new AppException("Item is already signed.", 409);

        item.ManualResult               = req.Result;
        item.ManualResultNotes          = req.Notes;
        item.ManualResultUnit           = req.Unit;
        item.ManualResultReferenceRange = req.ReferenceRange;
        item.ManualResultFlag           = ComputeFlag(req.Result, req.ReferenceRange);
        item.Status                     = LabOrderItemStatus.Resulted;
        item.ResultedAt                 = DateTime.UtcNow;

        UpdateOrderStatus(item.LabOrder);
        await _db.SaveChangesAsync(ct);

        return ToItemResponse(item);
    }

    // ── Sign item ─────────────────────────────────────────────────────────────

    public async Task<LabOrderItemResponse> SignItemAsync(Guid labOrderItemId, CancellationToken ct)
    {
        var item = await _db.LabOrderItems
            .Include(i => i.LabOrder)
                .ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(i => i.LabOrderItemId == labOrderItemId, ct)
            ?? throw new NotFoundException("LabOrderItem", labOrderItemId);

        if (item.Status != LabOrderItemStatus.Resulted)
            throw new AppException($"Item must be in Resulted status before signing (current: {item.Status}).", 409);

        item.Status          = LabOrderItemStatus.Signed;
        item.SignedAt        = DateTime.UtcNow;
        item.SignedByUserId  = _currentUser.UserId;

        UpdateOrderStatus(item.LabOrder);
        await _db.SaveChangesAsync(ct);

        return ToItemResponse(item);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates the parent order status based on its items.
    /// Called after any item status change that could complete or sign the order.
    /// </summary>
    private static void UpdateOrderStatus(LabOrder order)
    {
        var items = order.Items.ToList();
        if (items.Count == 0) return;

        var allSigned   = items.All(i => i.Status == LabOrderItemStatus.Signed);
        var allResulted = items.All(i => i.Status is LabOrderItemStatus.Resulted
                                                   or LabOrderItemStatus.Signed);
        var anyResulted = items.Any(i => i.Status is LabOrderItemStatus.Resulted
                                                    or LabOrderItemStatus.Signed);

        order.Status = allSigned   ? LabOrderStatus.Signed
                     : allResulted ? LabOrderStatus.Completed
                     : anyResulted ? LabOrderStatus.PartiallyCompleted
                                   : LabOrderStatus.Active;
    }

    /// <summary>
    /// Computes an abnormal flag by comparing a numeric value to a "low-high" reference range string.
    /// Returns "H" (high), "L" (low), "N" (normal), or null if parsing fails or range is absent.
    /// </summary>
    private static string? ComputeFlag(string value, string? referenceRange)
    {
        if (string.IsNullOrWhiteSpace(referenceRange)) return null;

        // Find the dash that separates low from high (skip index 0 to handle a leading minus sign on 'low')
        var dashIdx = referenceRange.IndexOf('-', 1);
        if (dashIdx < 1) return null;

        var lowStr  = referenceRange[..dashIdx];
        var highStr = referenceRange[(dashIdx + 1)..];

        if (!double.TryParse(value,   System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var val))  return null;
        if (!double.TryParse(lowStr,  System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var low))  return null;
        if (!double.TryParse(highStr, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var high)) return null;

        if (val < low)  return "L";
        if (val > high) return "H";
        return "N";
    }

    /// <summary>Generates ACC-{YEAR}-{NNNNN} sequential per tenant per year.</summary>
    private async Task<string> GenerateAccessionNumberAsync(CancellationToken ct)
    {
        var year   = DateTime.UtcNow.Year;
        var prefix = $"ACC-{year}-";

        var last = await _db.LabOrderItems
            .Where(i => i.TenantId == _currentUser.TenantId
                     && i.AccessionNumber != null
                     && i.AccessionNumber.StartsWith(prefix))
            .Select(i => i.AccessionNumber!)
            .OrderByDescending(n => n)
            .FirstOrDefaultAsync(ct);

        var next = last == null
            ? 1
            : int.Parse(last[(prefix.Length)..]) + 1;

        return $"{prefix}{next:D5}";
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static LabTestCatalogResponse ToCatalogResponse(LabTestCatalog t) => new()
    {
        LabTestCatalogId     = t.LabTestCatalogId,
        TestCode             = t.TestCode,
        TestName             = t.TestName,
        Department           = t.Department,
        InstrumentType       = t.InstrumentType,
        IsManualEntry        = t.IsManualEntry,
        TatHours             = t.TatHours,
        DefaultUnit          = t.DefaultUnit,
        DefaultReferenceRange = t.DefaultReferenceRange,
    };

    private static LabOrderResponse ToResponse(LabOrder o)
    {
        var items      = o.Items.ToList();
        var incomplete = items.Count(i => i.Status is LabOrderItemStatus.Ordered
                                                     or LabOrderItemStatus.SampleReceived);
        var completed  = items.Count(i => i.Status == LabOrderItemStatus.Resulted);
        var signed     = items.Count(i => i.Status == LabOrderItemStatus.Signed);

        return new LabOrderResponse
        {
            LabOrderId           = o.LabOrderId,
            PatientId            = o.PatientId,
            PatientName          = $"{o.Patient.FirstName} {o.Patient.LastName}",
            PatientMrn           = o.Patient.MedicalRecordNumber,
            PatientGender        = o.Patient.Gender,
            PatientDob           = o.Patient.DateOfBirth,
            ConsultationId       = o.ConsultationId,
            BillId               = o.BillId,
            BillNumber           = o.Bill?.BillNumber,
            OrderingDoctorUserId = o.OrderingDoctorUserId,
            OrderingDoctorName   = $"Dr. {o.OrderingDoctor.FirstName} {o.OrderingDoctor.LastName}",
            Organisation         = o.Organisation,
            Status               = o.Status,
            Notes                = o.Notes,
            OrderedAt            = o.CreatedAt,
            IncompleteCount      = incomplete,
            CompletedCount       = completed,
            SignedCount          = signed,
            TestNames            = items.Select(i => i.TestName).ToList().AsReadOnly(),
        };
    }

    private static LabOrderDetailResponse ToDetailResponse(LabOrder o) => new()
    {
        LabOrderId           = o.LabOrderId,
        PatientId            = o.PatientId,
        PatientName          = $"{o.Patient.FirstName} {o.Patient.LastName}",
        PatientMrn           = o.Patient.MedicalRecordNumber,
        PatientGender        = o.Patient.Gender,
        PatientDob           = o.Patient.DateOfBirth,
        ConsultationId       = o.ConsultationId,
        BillId               = o.BillId,
        BillNumber           = o.Bill?.BillNumber,
        OrderingDoctorUserId = o.OrderingDoctorUserId,
        OrderingDoctorName   = $"Dr. {o.OrderingDoctor.FirstName} {o.OrderingDoctor.LastName}",
        Organisation         = o.Organisation,
        Status               = o.Status,
        Notes                = o.Notes,
        OrderedAt            = o.CreatedAt,
        IncompleteCount      = o.Items.Count(i => i.Status is LabOrderItemStatus.Ordered
                                                             or LabOrderItemStatus.SampleReceived),
        CompletedCount       = o.Items.Count(i => i.Status == LabOrderItemStatus.Resulted),
        SignedCount          = o.Items.Count(i => i.Status == LabOrderItemStatus.Signed),
        TestNames            = o.Items.Select(i => i.TestName).ToList().AsReadOnly(),
        Items                = o.Items.Select(ToItemResponse).ToList().AsReadOnly(),
    };

    private static LabOrderItemResponse ToItemResponse(LabOrderItem i)
    {
        var isTatExceeded = i.SampleReceivedAt.HasValue
                         && i.Status != LabOrderItemStatus.Resulted
                         && i.Status != LabOrderItemStatus.Signed
                         && DateTime.UtcNow > i.SampleReceivedAt.Value.AddHours(i.TatHours);

        return new LabOrderItemResponse
        {
            LabOrderItemId   = i.LabOrderItemId,
            LabTestCatalogId = i.LabTestCatalogId,
            TestName         = i.TestName,
            Department       = i.Department,
            InstrumentType   = i.InstrumentType,
            IsManualEntry    = i.IsManualEntry,
            TatHours         = i.TatHours,
            AccessionNumber  = i.AccessionNumber,
            Status           = i.Status,
            SampleReceivedAt = i.SampleReceivedAt,
            ResultedAt       = i.ResultedAt,
            SignedAt         = i.SignedAt,
            ManualResult               = i.ManualResult,
            ManualResultNotes          = i.ManualResultNotes,
            ManualResultUnit           = i.ManualResultUnit,
            ManualResultReferenceRange = i.ManualResultReferenceRange,
            ManualResultFlag           = i.ManualResultFlag,
            LabResultId                = i.LabResultId,
            IsTatExceeded              = isTatExceeded,
        };
    }
}
