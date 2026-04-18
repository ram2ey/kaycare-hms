using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KayCare.Infrastructure.Services;

public class BillingPdfService : IBillingPdfService
{
    private readonly AppDbContext             _db;
    private readonly ITenantContext           _tenantContext;
    private readonly IFacilitySettingsService _facility;

    public BillingPdfService(AppDbContext db, ITenantContext tenantContext, IFacilitySettingsService facility)
    {
        _db            = db;
        _tenantContext = tenantContext;
        _facility      = facility;
    }

    // ── Invoice ───────────────────────────────────────────────────────────────

    public async Task<byte[]?> GenerateBillInvoiceAsync(Guid billId, CancellationToken ct)
    {
        var bill = await _db.Bills
            .Include(b => b.Patient)
            .Include(b => b.CreatedBy)
            .Include(b => b.Items)
            .Include(b => b.Payments)
                .ThenInclude(p => p.ReceivedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BillId == billId, ct);

        if (bill == null) return null;

        var facilityInfo = await _facility.GetAsync(ct);
        var facilityName = facilityInfo?.FacilityName
            ?? (await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.TenantId == _tenantContext.TenantId, ct))?.TenantName
            ?? "Medical Facility";
        var logoBytes    = await _facility.GetLogoBytesAsync(ct);

        var patient      = bill.Patient;
        var dob          = patient.DateOfBirth;
        var age          = (int)((DateTime.UtcNow - dob.ToDateTime(TimeOnly.MinValue)).TotalDays / 365.25);

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
                            if (logoBytes != null)
                                col.Item().MaxWidth(80).MaxHeight(40).Image(logoBytes);
                            col.Item().Text(facilityName)
                               .FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                            if (!string.IsNullOrWhiteSpace(facilityInfo?.Address))
                                col.Item().Text(facilityInfo.Address).FontSize(8).FontColor(Colors.Grey.Darken1);
                            if (!string.IsNullOrWhiteSpace(facilityInfo?.Phone) || !string.IsNullOrWhiteSpace(facilityInfo?.Email))
                                col.Item().Text(string.Join("  ·  ", new[] { facilityInfo?.Phone, facilityInfo?.Email }.Where(s => !string.IsNullOrWhiteSpace(s))!))
                                   .FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(160).AlignRight().Column(col =>
                        {
                            col.Item().Text("INVOICE")
                               .FontSize(14).Bold().FontColor(Colors.Grey.Darken2);
                            col.Item().Text(bill.BillNumber)
                               .FontSize(10).FontFamily("Courier New").FontColor(Colors.Blue.Darken3);
                        });
                    });

                    header.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(Colors.Blue.Darken3);

                    header.Item().PaddingTop(6).Background(Colors.Grey.Lighten4).Padding(6).Row(row =>
                    {
                        // Patient info
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("BILL TO").Bold().FontSize(8).FontColor(Colors.Grey.Darken1);
                            col.Item().PaddingTop(3).Text($"{patient.FirstName} {patient.LastName}").Bold();
                            col.Item().Text($"MRN: {patient.MedicalRecordNumber}").FontColor(Colors.Grey.Darken1);
                            col.Item().Text($"DOB: {dob:dd-MMM-yyyy}  ({age} yrs)").FontColor(Colors.Grey.Darken1);
                            if (!string.IsNullOrWhiteSpace(patient.PhoneNumber))
                                col.Item().Text($"Phone: {patient.PhoneNumber}").FontColor(Colors.Grey.Darken1);
                        });

                        // Invoice info
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("INVOICE DETAILS").Bold().FontSize(8).FontColor(Colors.Grey.Darken1);
                            col.Item().PaddingTop(3).Row(r =>
                            {
                                r.ConstantItem(90).Text("Invoice #:").Bold();
                                r.RelativeItem().Text(bill.BillNumber).FontFamily("Courier New");
                            });
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(90).Text("Date:").Bold();
                                r.RelativeItem().Text(bill.CreatedAt.ToString("dd-MMM-yyyy"));
                            });
                            if (bill.IssuedAt.HasValue)
                            {
                                col.Item().Row(r =>
                                {
                                    r.ConstantItem(90).Text("Issued:").Bold();
                                    r.RelativeItem().Text(bill.IssuedAt.Value.ToString("dd-MMM-yyyy"));
                                });
                            }
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(90).Text("Status:").Bold();
                                r.RelativeItem().Text(bill.Status);
                            });
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(90).Text("Prepared by:").Bold();
                                r.RelativeItem().Text($"{bill.CreatedBy.FirstName} {bill.CreatedBy.LastName}");
                            });
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(90).Text("Generated:").Bold();
                                r.RelativeItem().Text(DateTime.Now.ToString("dd-MMM-yyyy HH:mm"))
                                   .FontColor(Colors.Grey.Darken1);
                            });
                        });
                    });

                    header.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                });

                // ── Content ───────────────────────────────────────────────────
                page.Content().PaddingTop(10).Column(content =>
                {
                    // Line items table
                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(22);   // #
                            cols.RelativeColumn(4);    // Description
                            cols.RelativeColumn(1.5f); // Category
                            cols.ConstantColumn(36);   // Qty
                            cols.ConstantColumn(75);   // Unit Price
                            cols.ConstantColumn(75);   // Total
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

                        var items = bill.Items.ToList();
                        for (var i = 0; i < items.Count; i++)
                        {
                            var item = items[i];
                            table.Cell().Element(DataCell).Text((i + 1).ToString()).FontColor(Colors.Grey.Medium);
                            table.Cell().Element(DataCell).Text(item.Description);
                            table.Cell().Element(DataCell).Text(item.Category ?? "—").FontColor(Colors.Grey.Darken1);
                            table.Cell().Element(DataCell).AlignCenter().Text(item.Quantity.ToString());
                            table.Cell().Element(DataCell).AlignRight().Text($"GHS {item.UnitPrice:0.00}");
                            table.Cell().Element(DataCell).AlignRight().Text($"GHS {item.TotalPrice:0.00}").Bold();
                        }
                    });

                    // Totals block
                    content.Item().PaddingTop(6).AlignRight().Width(220).Column(totals =>
                    {
                        totals.Item().Background(Colors.Grey.Lighten4).Padding(8).Column(col =>
                        {
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Subtotal").FontColor(Colors.Grey.Darken2);
                                r.ConstantItem(90).AlignRight().Text($"GHS {bill.TotalAmount:0.00}");
                            });
                            col.Item().PaddingTop(2).Row(r =>
                            {
                                r.RelativeItem().Text("Amount Paid").FontColor(Colors.Grey.Darken2);
                                r.ConstantItem(90).AlignRight().Text($"GHS {bill.PaidAmount:0.00}").FontColor(Colors.Green.Darken2);
                            });
                            col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
                            col.Item().PaddingTop(4).Row(r =>
                            {
                                r.RelativeItem().Text("Balance Due").Bold().FontSize(10);
                                r.ConstantItem(90).AlignRight().Text($"GHS {bill.BalanceDue:0.00}")
                                   .Bold().FontSize(10)
                                   .FontColor(bill.BalanceDue > 0 ? Colors.Red.Darken2 : Colors.Green.Darken2);
                            });
                        });
                    });

                    // Payments summary (if any)
                    if (bill.Payments.Count > 0)
                    {
                        content.Item().PaddingTop(14).Column(col =>
                        {
                            col.Item().Text("Payment History").Bold().FontSize(9);
                            col.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(1.5f); // Date
                                    cols.RelativeColumn(1.5f); // Method
                                    cols.RelativeColumn(2);    // Reference
                                    cols.RelativeColumn(2);    // Received By
                                    cols.ConstantColumn(80);   // Amount
                                });

                                static IContainer HCell(IContainer c) =>
                                    c.Background(Colors.Grey.Lighten3).Padding(4);

                                table.Header(h =>
                                {
                                    h.Cell().Element(HCell).Text("Date").Bold();
                                    h.Cell().Element(HCell).Text("Method").Bold();
                                    h.Cell().Element(HCell).Text("Reference").Bold();
                                    h.Cell().Element(HCell).Text("Received By").Bold();
                                    h.Cell().Element(HCell).AlignRight().Text("Amount").Bold();
                                });

                                static IContainer DCell(IContainer c) =>
                                    c.BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(4);

                                foreach (var p in bill.Payments.OrderBy(p => p.PaymentDate))
                                {
                                    table.Cell().Element(DCell).Text(p.PaymentDate.ToString("dd-MMM-yyyy"));
                                    table.Cell().Element(DCell).Text(p.PaymentMethod);
                                    table.Cell().Element(DCell).Text(p.Reference ?? "—").FontColor(Colors.Grey.Darken1);
                                    table.Cell().Element(DCell).Text($"{p.ReceivedBy.FirstName} {p.ReceivedBy.LastName}");
                                    table.Cell().Element(DCell).AlignRight().Text($"GHS {p.Amount:0.00}").FontColor(Colors.Green.Darken2);
                                }
                            });
                        });
                    }

                    // Notes
                    if (!string.IsNullOrWhiteSpace(bill.Notes))
                    {
                        content.Item().PaddingTop(14).Column(col =>
                        {
                            col.Item().Text("Notes").Bold().FontSize(9);
                            col.Item().PaddingTop(3).Background(Colors.Grey.Lighten4).Padding(6)
                               .Text(bill.Notes).FontSize(8.5f);
                        });
                    }
                });

                // ── Footer ────────────────────────────────────────────────────
                page.Footer().Column(footer =>
                {
                    footer.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                    footer.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem()
                           .Text("This is a computer-generated invoice. For queries, contact the billing department.")
                           .FontSize(7).FontColor(Colors.Grey.Medium);
                        row.ConstantItem(80).AlignRight().AlignBottom()
                           .Text(x =>
                           {
                               x.Span("Page ").FontSize(7).FontColor(Colors.Grey.Medium);
                               x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                               x.Span(" of ").FontSize(7).FontColor(Colors.Grey.Medium);
                               x.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                           });
                    });
                });
            });
        }).GeneratePdf();
    }

    // ── Receipt ───────────────────────────────────────────────────────────────

    public async Task<byte[]?> GeneratePaymentReceiptAsync(Guid paymentId, CancellationToken ct)
    {
        var payment = await _db.Payments
            .Include(p => p.Bill)
                .ThenInclude(b => b.Patient)
            .Include(p => p.Bill)
                .ThenInclude(b => b.Items)
            .Include(p => p.ReceivedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId, ct);

        if (payment == null) return null;

        var facilityInfo = await _facility.GetAsync(ct);
        var facilityName = facilityInfo?.FacilityName
            ?? (await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.TenantId == _tenantContext.TenantId, ct))?.TenantName
            ?? "Medical Facility";
        var logoBytes    = await _facility.GetLogoBytesAsync(ct);

        var bill         = payment.Bill;
        var patient      = bill.Patient;
        var receiptRef   = paymentId.ToString("N")[..8].ToUpperInvariant();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                // Half-page receipt (A5 width, shorter height)
                page.Size(PageSizes.A5);
                page.MarginHorizontal(1.5f, Unit.Centimetre);
                page.MarginVertical(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(9).FontFamily("Arial"));

                // ── Header ────────────────────────────────────────────────────
                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            if (logoBytes != null)
                                col.Item().MaxWidth(60).MaxHeight(30).Image(logoBytes);
                            col.Item().Text(facilityName)
                               .FontSize(13).Bold().FontColor(Colors.Blue.Darken3);
                            if (!string.IsNullOrWhiteSpace(facilityInfo?.Phone) || !string.IsNullOrWhiteSpace(facilityInfo?.Email))
                                col.Item().Text(string.Join("  ·  ", new[] { facilityInfo?.Phone, facilityInfo?.Email }.Where(s => !string.IsNullOrWhiteSpace(s))!))
                                   .FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(130).AlignRight().Column(col =>
                        {
                            col.Item().Text("PAYMENT RECEIPT")
                               .FontSize(10).Bold().FontColor(Colors.Grey.Darken2);
                            col.Item().Text($"Ref: {receiptRef}")
                               .FontSize(8).FontFamily("Courier New").FontColor(Colors.Blue.Darken3);
                        });
                    });
                    header.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(Colors.Blue.Darken3);
                });

                // ── Content ───────────────────────────────────────────────────
                page.Content().PaddingTop(10).Column(content =>
                {
                    // Payment details box
                    content.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(col =>
                    {
                        void Row(string label, string value, bool bold = false)
                        {
                            col.Item().PaddingBottom(3).Row(r =>
                            {
                                r.ConstantItem(110).Text(label).Bold().FontColor(Colors.Grey.Darken2);
                                if (bold)
                                    r.RelativeItem().Text(value).Bold();
                                else
                                    r.RelativeItem().Text(value);
                            });
                        }

                        Row("Patient:", $"{patient.FirstName} {patient.LastName}");
                        Row("MRN:", patient.MedicalRecordNumber);
                        Row("Invoice #:", bill.BillNumber);
                        Row("Payment Date:", payment.PaymentDate.ToString("dd-MMM-yyyy HH:mm"));
                        Row("Payment Method:", payment.PaymentMethod);
                        if (!string.IsNullOrWhiteSpace(payment.Reference))
                            Row("Reference:", payment.Reference);
                        Row("Received By:", $"{payment.ReceivedBy.FirstName} {payment.ReceivedBy.LastName}");
                    });

                    // Amount highlight
                    content.Item().PaddingTop(8).Background(Colors.Green.Lighten4).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Text("Amount Received").Bold().FontSize(11);
                        row.ConstantItem(120).AlignRight()
                           .Text($"GHS {payment.Amount:0.00}")
                           .Bold().FontSize(14).FontColor(Colors.Green.Darken3);
                    });

                    // Running totals
                    content.Item().PaddingTop(8).AlignRight().Width(200).Column(totals =>
                    {
                        totals.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Invoice Total").FontColor(Colors.Grey.Darken2);
                            r.ConstantItem(90).AlignRight().Text($"GHS {bill.TotalAmount:0.00}");
                        });
                        totals.Item().PaddingTop(2).Row(r =>
                        {
                            r.RelativeItem().Text("Total Paid to Date").FontColor(Colors.Grey.Darken2);
                            r.ConstantItem(90).AlignRight().Text($"GHS {bill.PaidAmount:0.00}").FontColor(Colors.Green.Darken2);
                        });
                        totals.Item().PaddingTop(2).Row(r =>
                        {
                            r.RelativeItem().Text("Balance Remaining").Bold();
                            r.ConstantItem(90).AlignRight().Text($"GHS {bill.BalanceDue:0.00}")
                               .Bold()
                               .FontColor(bill.BalanceDue > 0 ? Colors.Red.Darken2 : Colors.Green.Darken2);
                        });
                    });

                    if (!string.IsNullOrWhiteSpace(payment.Notes))
                    {
                        content.Item().PaddingTop(10).Column(col =>
                        {
                            col.Item().Text("Notes").Bold().FontSize(9);
                            col.Item().PaddingTop(3).Text(payment.Notes).FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                        });
                    }

                    // Thank-you line
                    content.Item().PaddingTop(14).AlignCenter()
                       .Text("Thank you for your payment.")
                       .FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
                });

                // ── Footer ────────────────────────────────────────────────────
                page.Footer().Column(footer =>
                {
                    footer.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                    footer.Item().PaddingTop(5)
                       .Text($"Generated: {DateTime.Now:dd-MMM-yyyy HH:mm}  ·  This is a computer-generated receipt.")
                       .FontSize(7).FontColor(Colors.Grey.Medium).AlignCenter();
                });
            });
        }).GeneratePdf();
    }
}
