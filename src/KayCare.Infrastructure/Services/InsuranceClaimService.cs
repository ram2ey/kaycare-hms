using KayCare.Core.Constants;
using KayCare.Core.DTOs.Billing;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KayCare.Infrastructure.Services;

public class InsuranceClaimService : IInsuranceClaimService
{
    private readonly AppDbContext        _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext      _tenantContext;

    public InsuranceClaimService(AppDbContext db, ICurrentUserService currentUser, ITenantContext tenantContext)
    {
        _db            = db;
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<InsuranceClaimResponse> CreateAsync(CreateClaimRequest req, CancellationToken ct = default)
    {
        var bill = await _db.Bills
            .Include(b => b.Patient)
            .FirstOrDefaultAsync(b => b.BillId == req.BillId, ct)
            ?? throw new NotFoundException(nameof(Bill), req.BillId);

        if (bill.Status == BillStatus.Draft)
            throw new AppException("Cannot create a claim against a Draft bill. Issue the bill first.", 400);

        if (bill.Status == BillStatus.Paid)
            throw new AppException("Cannot create a claim against a fully paid bill.", 400);

        var payer = await _db.Payers.FirstOrDefaultAsync(p => p.PayerId == req.PayerId, ct)
            ?? throw new NotFoundException(nameof(Payer), req.PayerId);

        if (!payer.IsActive)
            throw new AppException("The selected payer is inactive.", 400);

        // Prevent duplicate open claim for the same bill/payer
        var duplicate = await _db.InsuranceClaims.AnyAsync(
            c => c.BillId == req.BillId &&
                 c.PayerId == req.PayerId &&
                 c.Status != ClaimStatus.Rejected &&
                 c.Status != ClaimStatus.Cancelled, ct);

        if (duplicate)
            throw new AppException("An open claim already exists for this bill and payer.", 409);

        var claimNumber = await GenerateClaimNumberAsync(ct);
        var claimAmount = req.ClaimAmount ?? bill.BalanceDue;

        if (claimAmount <= 0)
            throw new AppException("Claim amount must be greater than zero.", 400);

        if (claimAmount > bill.BalanceDue)
            throw new AppException($"Claim amount ({claimAmount:F2}) cannot exceed the bill balance due ({bill.BalanceDue:F2}).", 400);

        var claim = new InsuranceClaim
        {
            ClaimNumber     = claimNumber,
            BillId          = req.BillId,
            PayerId         = req.PayerId,
            PatientId       = bill.PatientId,
            CreatedByUserId = _currentUser.UserId,
            NhisNumber      = bill.Patient.NhisNumber,
            Status          = ClaimStatus.Draft,
            ClaimAmount     = claimAmount,
            Notes           = req.Notes?.Trim()
        };

        _db.InsuranceClaims.Add(claim);
        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(claim.ClaimId, ct)
            ?? throw new InvalidOperationException("Failed to load created claim.");
    }

    // ── List ──────────────────────────────────────────────────────────────────

    public async Task<List<InsuranceClaimResponse>> GetAllAsync(
        string? status, Guid? payerId, Guid? patientId, CancellationToken ct = default)
    {
        var query = _db.InsuranceClaims
            .Include(c => c.Bill)
            .Include(c => c.Payer)
            .Include(c => c.Patient)
            .Include(c => c.CreatedBy)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);

        if (payerId.HasValue)
            query = query.Where(c => c.PayerId == payerId.Value);

        if (patientId.HasValue)
            query = query.Where(c => c.PatientId == patientId.Value);

        var claims = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        return claims.Select(MapToResponse).ToList();
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    public async Task<InsuranceClaimResponse?> GetByIdAsync(Guid claimId, CancellationToken ct = default)
        => await LoadDetailAsync(claimId, ct);

    // ── Submit ────────────────────────────────────────────────────────────────

    public async Task<InsuranceClaimResponse> SubmitAsync(Guid claimId, CancellationToken ct = default)
    {
        var claim = await _db.InsuranceClaims.FirstOrDefaultAsync(c => c.ClaimId == claimId, ct)
            ?? throw new NotFoundException(nameof(InsuranceClaim), claimId);

        if (claim.Status != ClaimStatus.Draft)
            throw new AppException($"Only Draft claims can be submitted. Current status: {claim.Status}.", 409);

        claim.Status      = ClaimStatus.Submitted;
        claim.SubmittedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(claimId, ct)
            ?? throw new InvalidOperationException("Failed to load claim after submit.");
    }

    // ── Approve ───────────────────────────────────────────────────────────────

    public async Task<InsuranceClaimResponse> ApproveAsync(
        Guid claimId, ApproveClaimRequest req, CancellationToken ct = default)
    {
        var claim = await _db.InsuranceClaims
            .Include(c => c.Bill)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId, ct)
            ?? throw new NotFoundException(nameof(InsuranceClaim), claimId);

        if (claim.Status != ClaimStatus.Submitted)
            throw new AppException($"Only Submitted claims can be approved. Current status: {claim.Status}.", 409);

        if (req.ApprovedAmount > claim.ClaimAmount)
            throw new AppException(
                $"Approved amount ({req.ApprovedAmount:F2}) cannot exceed the claimed amount ({claim.ClaimAmount:F2}).", 400);

        var bill = claim.Bill;

        if (req.ApprovedAmount > bill.BalanceDue)
            throw new AppException(
                $"Approved amount ({req.ApprovedAmount:F2}) cannot exceed the bill balance due ({bill.BalanceDue:F2}).", 400);

        // Auto-create a payment on the bill for the approved amount
        var payment = new Payment
        {
            BillId           = bill.BillId,
            Amount           = req.ApprovedAmount,
            PaymentMethod    = "Insurance",
            Reference        = claim.ClaimNumber,
            ReceivedByUserId = _currentUser.UserId,
            PaymentDate      = DateTime.UtcNow,
            Notes            = $"Insurance claim {claim.ClaimNumber} approved"
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct); // flush to get PaymentId

        bill.PaidAmount += req.ApprovedAmount;
        bill.Status = bill.PaidAmount >= (bill.TotalAmount + bill.AdjustmentTotal - bill.DiscountAmount - bill.WriteOffAmount)
            ? BillStatus.Paid
            : BillStatus.PartiallyPaid;

        claim.ApprovedAmount = req.ApprovedAmount;
        claim.Status         = req.ApprovedAmount >= claim.ClaimAmount
            ? ClaimStatus.Approved
            : ClaimStatus.PartiallyApproved;
        claim.ResponseAt     = DateTime.UtcNow;
        claim.PaymentId      = payment.PaymentId;
        if (!string.IsNullOrWhiteSpace(req.Notes))
            claim.Notes = req.Notes.Trim();

        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(claimId, ct)
            ?? throw new InvalidOperationException("Failed to load claim after approval.");
    }

    // ── Reject ────────────────────────────────────────────────────────────────

    public async Task<InsuranceClaimResponse> RejectAsync(
        Guid claimId, RejectClaimRequest req, CancellationToken ct = default)
    {
        var claim = await _db.InsuranceClaims.FirstOrDefaultAsync(c => c.ClaimId == claimId, ct)
            ?? throw new NotFoundException(nameof(InsuranceClaim), claimId);

        if (claim.Status != ClaimStatus.Submitted)
            throw new AppException($"Only Submitted claims can be rejected. Current status: {claim.Status}.", 409);

        claim.Status          = ClaimStatus.Rejected;
        claim.RejectionReason = req.RejectionReason.Trim();
        claim.ResponseAt      = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(req.Notes))
            claim.Notes = req.Notes.Trim();

        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(claimId, ct)
            ?? throw new InvalidOperationException("Failed to load claim after rejection.");
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    public async Task<InsuranceClaimResponse> CancelAsync(Guid claimId, CancellationToken ct = default)
    {
        var claim = await _db.InsuranceClaims.FirstOrDefaultAsync(c => c.ClaimId == claimId, ct)
            ?? throw new NotFoundException(nameof(InsuranceClaim), claimId);

        if (claim.Status == ClaimStatus.Approved || claim.Status == ClaimStatus.PartiallyApproved)
            throw new AppException("Approved claims cannot be cancelled. Raise a credit note instead.", 409);

        if (claim.Status == ClaimStatus.Cancelled)
            throw new AppException("Claim is already cancelled.", 409);

        claim.Status = ClaimStatus.Cancelled;

        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(claimId, ct)
            ?? throw new InvalidOperationException("Failed to load claim after cancellation.");
    }

    // ── Claim PDF ─────────────────────────────────────────────────────────────

    public async Task<byte[]?> GenerateClaimPdfAsync(Guid claimId, CancellationToken ct = default)
    {
        var claim = await _db.InsuranceClaims
            .Include(c => c.Bill)
                .ThenInclude(b => b.Items)
            .Include(c => c.Bill)
                .ThenInclude(b => b.Payer)
            .Include(c => c.Payer)
            .Include(c => c.Patient)
            .Include(c => c.CreatedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClaimId == claimId, ct);

        if (claim == null) return null;

        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TenantId == _tenantContext.TenantId, ct);

        var facilityName = tenant?.TenantName ?? "Medical Facility";
        var patient      = claim.Patient;
        var bill         = claim.Bill;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(1.8f, Unit.Centimetre);
                page.MarginVertical(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(9).FontFamily("Arial"));

                // ── Header ────────────────────────────────────────────────────
                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(facilityName)
                               .FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                            col.Item().Text("Insurance Claim Form")
                               .FontSize(10).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(160).AlignRight().Column(col =>
                        {
                            col.Item().Text("INSURANCE CLAIM")
                               .FontSize(12).Bold().FontColor(Colors.Grey.Darken2);
                            col.Item().Text(claim.ClaimNumber)
                               .FontSize(10).FontFamily("Courier New").FontColor(Colors.Blue.Darken3);
                        });
                    });

                    header.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(Colors.Blue.Darken3);

                    header.Item().PaddingTop(6).Background(Colors.Grey.Lighten4).Padding(6).Row(row =>
                    {
                        // Patient block
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("PATIENT").Bold().FontSize(8).FontColor(Colors.Grey.Darken1);
                            col.Item().PaddingTop(3).Text($"{patient.FirstName} {patient.LastName}").Bold();
                            col.Item().Text($"MRN: {patient.MedicalRecordNumber}").FontColor(Colors.Grey.Darken1);
                            if (!string.IsNullOrEmpty(claim.NhisNumber))
                                col.Item().Text($"NHIS #: {claim.NhisNumber}").FontColor(Colors.Grey.Darken1);
                            if (!string.IsNullOrEmpty(patient.InsuranceProvider))
                                col.Item().Text($"Insurance: {patient.InsuranceProvider}").FontColor(Colors.Grey.Darken1);
                            if (!string.IsNullOrEmpty(patient.InsurancePolicyNumber))
                                col.Item().Text($"Policy #: {patient.InsurancePolicyNumber}").FontColor(Colors.Grey.Darken1);
                        });

                        // Claim block
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("CLAIM DETAILS").Bold().FontSize(8).FontColor(Colors.Grey.Darken1);
                            col.Item().PaddingTop(3).Row(r =>
                            {
                                r.ConstantItem(90).Text("Claim #:").Bold();
                                r.RelativeItem().Text(claim.ClaimNumber).FontFamily("Courier New");
                            });
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(90).Text("Invoice #:").Bold();
                                r.RelativeItem().Text(bill.BillNumber).FontFamily("Courier New");
                            });
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(90).Text("Payer:").Bold();
                                r.RelativeItem().Text(claim.Payer.Name);
                            });
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(90).Text("Status:").Bold();
                                r.RelativeItem().Text(claim.Status);
                            });
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(90).Text("Date:").Bold();
                                r.RelativeItem().Text(claim.CreatedAt.ToString("dd-MMM-yyyy"));
                            });
                            if (claim.SubmittedAt.HasValue)
                            {
                                col.Item().Row(r =>
                                {
                                    r.ConstantItem(90).Text("Submitted:").Bold();
                                    r.RelativeItem().Text(claim.SubmittedAt.Value.ToString("dd-MMM-yyyy"));
                                });
                            }
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(90).Text("Prepared by:").Bold();
                                r.RelativeItem().Text($"{claim.CreatedBy.FirstName} {claim.CreatedBy.LastName}");
                            });
                        });
                    });

                    header.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                });

                // ── Content ───────────────────────────────────────────────────
                page.Content().PaddingTop(10).Column(content =>
                {
                    // Services / line items
                    content.Item().Text("SERVICES / ITEMS CLAIMED").Bold().FontSize(9);
                    content.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(22);
                            cols.RelativeColumn(4);
                            cols.RelativeColumn(1.5f);
                            cols.ConstantColumn(36);
                            cols.ConstantColumn(75);
                            cols.ConstantColumn(75);
                        });

                        static IContainer HeaderCell(IContainer c) =>
                            c.Background(Colors.Blue.Lighten4).Padding(4);

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("#").Bold();
                            h.Cell().Element(HeaderCell).Text("Description").Bold();
                            h.Cell().Element(HeaderCell).Text("Category").Bold();
                            h.Cell().Element(HeaderCell).AlignCenter().Text("Qty").Bold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("Unit Price").Bold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("Total").Bold();
                        });

                        static IContainer DataCell(IContainer c) =>
                            c.BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(4);

                        var idx = 1;
                        foreach (var item in bill.Items)
                        {
                            var lineTotal = item.Quantity * item.UnitPrice;
                            table.Cell().Element(DataCell).Text(idx++.ToString());
                            table.Cell().Element(DataCell).Text(item.Description);
                            table.Cell().Element(DataCell).Text(item.Category ?? "—").FontColor(Colors.Grey.Darken1);
                            table.Cell().Element(DataCell).AlignCenter().Text(item.Quantity.ToString());
                            table.Cell().Element(DataCell).AlignRight().Text($"GHS {item.UnitPrice:N2}");
                            table.Cell().Element(DataCell).AlignRight().Text($"GHS {lineTotal:N2}");
                        }
                    });

                    // Totals
                    content.Item().PaddingTop(10).AlignRight().Column(totals =>
                    {
                        static void TotalRow(ColumnDescriptor col, string label, string value)
                        {
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(140).AlignRight().Text(label).FontColor(Colors.Grey.Darken1);
                                r.ConstantItem(90).AlignRight().Text(value);
                            });
                        }

                        TotalRow(totals, "Bill Total:", $"GHS {bill.TotalAmount:N2}");
                        if (bill.DiscountAmount > 0)
                            TotalRow(totals, "Discount:", $"- GHS {bill.DiscountAmount:N2}");
                        if (bill.AdjustmentTotal != 0)
                            TotalRow(totals, "Adjustments:", $"GHS {bill.AdjustmentTotal:N2}");
                        if (bill.WriteOffAmount > 0)
                            TotalRow(totals, "Write-off:", $"- GHS {bill.WriteOffAmount:N2}");
                        TotalRow(totals, "Already Paid:", $"- GHS {bill.PaidAmount:N2}");
                        TotalRow(totals, "Balance Due:", $"GHS {bill.BalanceDue:N2}");
                        totals.Item().PaddingTop(2).LineHorizontal(1f).LineColor(Colors.Blue.Darken3);
                        totals.Item().PaddingTop(2).Row(r =>
                        {
                            r.ConstantItem(140).AlignRight().Text("AMOUNT CLAIMED:").Bold();
                            r.ConstantItem(90).AlignRight().Text($"GHS {claim.ClaimAmount:N2}").Bold();
                        });

                        if (claim.ApprovedAmount.HasValue)
                        {
                            totals.Item().PaddingTop(4).Row(r =>
                            {
                                r.ConstantItem(140).AlignRight().Text("APPROVED AMOUNT:").Bold()
                                   .FontColor(Colors.Green.Darken2);
                                r.ConstantItem(90).AlignRight().Text($"GHS {claim.ApprovedAmount.Value:N2}").Bold()
                                   .FontColor(Colors.Green.Darken2);
                            });
                        }

                        if (!string.IsNullOrEmpty(claim.RejectionReason))
                        {
                            totals.Item().PaddingTop(4).Text($"Rejection reason: {claim.RejectionReason}")
                               .FontColor(Colors.Red.Darken2).Italic();
                        }
                    });

                    // Notes
                    if (!string.IsNullOrWhiteSpace(claim.Notes))
                    {
                        content.Item().PaddingTop(12).Column(notes =>
                        {
                            notes.Item().Text("Notes").Bold();
                            notes.Item().PaddingTop(2).Text(claim.Notes).FontColor(Colors.Grey.Darken1);
                        });
                    }

                    // Declaration
                    content.Item().PaddingTop(20).Text(
                        "I hereby certify that the services listed above were rendered to the named patient " +
                        "and that the information provided is accurate and complete.")
                        .Italic().FontSize(8).FontColor(Colors.Grey.Darken1);

                    content.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Darken1);
                            col.Item().PaddingTop(2).Text("Authorised Signature / Stamp").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(30);
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Darken1);
                            col.Item().PaddingTop(2).Text("Date").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                    });
                });

                // ── Footer ────────────────────────────────────────────────────
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.Span(" of ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.Span($"  •  Generated {DateTime.Now:dd-MMM-yyyy HH:mm}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<InsuranceClaimResponse?> LoadDetailAsync(Guid claimId, CancellationToken ct)
    {
        var claim = await _db.InsuranceClaims
            .Include(c => c.Bill)
            .Include(c => c.Payer)
            .Include(c => c.Patient)
            .Include(c => c.CreatedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClaimId == claimId, ct);

        return claim == null ? null : MapToResponse(claim);
    }

    private static InsuranceClaimResponse MapToResponse(InsuranceClaim c) => new()
    {
        ClaimId         = c.ClaimId,
        ClaimNumber     = c.ClaimNumber,
        BillId          = c.BillId,
        BillNumber      = c.Bill.BillNumber,
        PayerId         = c.PayerId,
        PayerName       = c.Payer.Name,
        PayerType       = c.Payer.Type,
        PatientId       = c.PatientId,
        PatientName     = $"{c.Patient.FirstName} {c.Patient.LastName}",
        PatientMrn      = c.Patient.MedicalRecordNumber,
        NhisNumber      = c.NhisNumber,
        Status          = c.Status,
        ClaimAmount     = c.ClaimAmount,
        ApprovedAmount  = c.ApprovedAmount,
        RejectionReason = c.RejectionReason,
        Notes           = c.Notes,
        SubmittedAt     = c.SubmittedAt,
        ResponseAt      = c.ResponseAt,
        PaymentId       = c.PaymentId,
        CreatedByName   = $"{c.CreatedBy.FirstName} {c.CreatedBy.LastName}",
        CreatedAt       = c.CreatedAt,
        UpdatedAt       = c.UpdatedAt
    };

    private async Task<string> GenerateClaimNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _db.InsuranceClaims
            .CountAsync(c => c.CreatedAt.Year == year, ct);
        return $"CLM-{year}-{(count + 1):D5}";
    }
}
