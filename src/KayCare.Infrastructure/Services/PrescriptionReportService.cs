using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KayCare.Infrastructure.Services;

public class PrescriptionReportService : IPrescriptionReportService
{
    private readonly AppDbContext             _db;
    private readonly ITenantContext           _tenantContext;
    private readonly IFacilitySettingsService _facility;

    public PrescriptionReportService(AppDbContext db, ITenantContext tenantContext, IFacilitySettingsService facility)
    {
        _db            = db;
        _tenantContext = tenantContext;
        _facility      = facility;
    }

    public async Task<byte[]?> GeneratePrescriptionReportAsync(Guid prescriptionId, CancellationToken ct)
    {
        var rx = await _db.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.PrescribedBy)
            .Include(p => p.DispensedBy)
            .Include(p => p.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId, ct);

        if (rx == null) return null;

        var facilityInfo = await _facility.GetAsync(ct);
        var facilityName = facilityInfo?.FacilityName
            ?? (await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.TenantId == _tenantContext.TenantId, ct))?.TenantName
            ?? "Medical Facility";
        var logoBytes    = await _facility.GetLogoBytesAsync(ct);
        var patient      = rx.Patient;
        var dob          = patient.DateOfBirth;
        var age          = (int)((DateTime.UtcNow - dob.ToDateTime(TimeOnly.MinValue)).TotalDays / 365.25);
        var doctorName   = $"Dr. {rx.PrescribedBy.FirstName} {rx.PrescribedBy.LastName}";
        var rxRef        = prescriptionId.ToString("N")[..8].ToUpperInvariant();
        var hasCs        = rx.Items.Any(i => i.IsControlledSubstance);

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
                            col.Item().Text("PRESCRIPTION")
                               .FontSize(10).Bold().FontColor(Colors.Grey.Darken2);
                            col.Item().Text($"Generated: {DateTime.Now:dd-MMM-yyyy HH:mm}")
                               .FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    header.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(Colors.Blue.Darken3);

                    header.Item().PaddingTop(6).Background(Colors.Grey.Lighten4).Padding(6).Row(row =>
                    {
                        // Patient demographics
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(55).Text("Patient:").Bold();
                                r.RelativeItem().Text($"{patient.FirstName} {patient.LastName}");
                            });
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(55).Text("MRN:").Bold();
                                r.RelativeItem().Text(patient.MedicalRecordNumber);
                            });
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(55).Text("DOB:").Bold();
                                r.RelativeItem().Text($"{dob:dd-MMM-yyyy}  ({age} yrs)");
                            });
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(55).Text("Gender:").Bold();
                                r.RelativeItem().Text(patient.Gender ?? "—");
                            });
                        });

                        // Prescription info
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(80).Text("Rx Ref:").Bold();
                                r.RelativeItem().Text(rxRef).FontFamily("Courier New");
                            });
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(80).Text("Date:").Bold();
                                r.RelativeItem().Text(rx.PrescriptionDate.ToString("dd-MMM-yyyy"));
                            });
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(80).Text("Prescribed by:").Bold();
                                r.RelativeItem().Text(doctorName);
                            });
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(80).Text("Status:").Bold();
                                r.RelativeItem().Text(rx.Status);
                            });
                        });
                    });

                    if (hasCs)
                    {
                        header.Item().PaddingTop(4)
                            .Background(Colors.Red.Lighten4).Padding(4)
                            .Text("⚠  This prescription contains one or more CONTROLLED SUBSTANCES.")
                            .FontSize(8).Bold().FontColor(Colors.Red.Darken3);
                    }

                    header.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                });

                // ── Content ───────────────────────────────────────────────────
                page.Content().PaddingTop(8).Column(content =>
                {
                    // Medications table
                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(22);    // #
                            cols.RelativeColumn(3);     // Medication
                            cols.RelativeColumn(1.5f);  // Strength / Form
                            cols.RelativeColumn(1.8f);  // Frequency
                            cols.ConstantColumn(48);    // Duration
                            cols.ConstantColumn(36);    // Qty
                            cols.ConstantColumn(40);    // Refills
                        });

                        static IContainer HeaderCell(IContainer c) =>
                            c.Background(Colors.Blue.Lighten4).Padding(4);

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("#").Bold();
                            h.Cell().Element(HeaderCell).Text("Medication").Bold();
                            h.Cell().Element(HeaderCell).Text("Strength / Form").Bold();
                            h.Cell().Element(HeaderCell).Text("Frequency").Bold();
                            h.Cell().Element(HeaderCell).Text("Duration").Bold();
                            h.Cell().Element(HeaderCell).Text("Qty").Bold();
                            h.Cell().Element(HeaderCell).Text("Refills").Bold();
                        });

                        static IContainer DataCell(IContainer c) =>
                            c.BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(4);

                        var items = rx.Items.ToList();
                        for (var i = 0; i < items.Count; i++)
                        {
                            var item = items[i];

                            table.Cell().Element(DataCell)
                                 .Text((i + 1).ToString()).FontColor(Colors.Grey.Medium);

                            // Medication name cell: brand + optional generic + CS badge
                            table.Cell().Element(DataCell).Column(col =>
                            {
                                col.Item().Row(r =>
                                {
                                    r.RelativeItem().Text(item.MedicationName).Bold();
                                    if (item.IsControlledSubstance)
                                        r.ConstantItem(22).Background(Colors.Red.Lighten4)
                                         .AlignCenter().AlignMiddle()
                                         .Text("CS").FontSize(7).Bold().FontColor(Colors.Red.Darken3);
                                });
                                if (!string.IsNullOrWhiteSpace(item.GenericName))
                                    col.Item().Text(item.GenericName)
                                       .FontSize(8).FontColor(Colors.Grey.Darken1).Italic();
                                if (!string.IsNullOrWhiteSpace(item.Instructions))
                                    col.Item().PaddingTop(2)
                                       .Text($"Note: {item.Instructions}")
                                       .FontSize(7.5f).FontColor(Colors.Grey.Darken2).Italic();
                            });

                            table.Cell().Element(DataCell)
                                 .Text($"{item.Strength}\n{item.DosageForm}").FontSize(8.5f);

                            table.Cell().Element(DataCell).Text(item.Frequency);

                            table.Cell().Element(DataCell)
                                 .Text($"{item.DurationDays} day{(item.DurationDays != 1 ? "s" : "")}");

                            table.Cell().Element(DataCell).AlignCenter()
                                 .Text(item.Quantity.ToString());

                            table.Cell().Element(DataCell).AlignCenter()
                                 .Text(item.Refills.ToString());
                        }
                    });

                    // Notes
                    if (!string.IsNullOrWhiteSpace(rx.Notes))
                    {
                        content.Item().PaddingTop(14).Column(col =>
                        {
                            col.Item().Text("Notes").Bold().FontSize(9);
                            col.Item().PaddingTop(3).Background(Colors.Grey.Lighten4).Padding(6)
                               .Text(rx.Notes).FontSize(8.5f);
                        });
                    }

                    // Dispensing info (if dispensed)
                    if (rx.DispensedBy != null && rx.DispensedAt.HasValue)
                    {
                        content.Item().PaddingTop(14).Column(col =>
                        {
                            col.Item().Text("Dispensing Record").Bold().FontSize(9);
                            col.Item().PaddingTop(3).Row(row =>
                            {
                                row.RelativeItem().Text(
                                    $"Dispensed by: {rx.DispensedBy.FirstName} {rx.DispensedBy.LastName}");
                                row.RelativeItem().AlignRight().Text(
                                    $"Dispensed at: {rx.DispensedAt.Value:dd-MMM-yyyy HH:mm}");
                            });
                        });
                    }
                });

                // ── Footer ────────────────────────────────────────────────────
                page.Footer().Column(footer =>
                {
                    footer.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                    footer.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text($"Prescriber Signature ({doctorName}): _______________________________");
                            col.Item().PaddingTop(2)
                               .Text("This is a computer-generated prescription. Valid only with authorised prescriber signature.")
                               .FontSize(7).FontColor(Colors.Grey.Medium);
                        });
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
}
