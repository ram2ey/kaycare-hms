using MediCloud.Core.Constants;
using MediCloud.Core.Entities;
using MediCloud.Core.Interfaces;
using MediCloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MediCloud.Infrastructure.Services;

public class LabReportService : ILabReportService
{
    private readonly AppDbContext    _db;
    private readonly ITenantContext  _tenantContext;

    public LabReportService(AppDbContext db, ITenantContext tenantContext)
    {
        _db            = db;
        _tenantContext = tenantContext;
    }

    public async Task<byte[]?> GenerateLabOrderReportAsync(Guid labOrderId, CancellationToken ct)
    {
        var order = await _db.LabOrders
            .Include(o => o.Patient)
            .Include(o => o.OrderingDoctor)
            .Include(o => o.Items)
                .ThenInclude(i => i.LabResult)
                    .ThenInclude(r => r!.Observations)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.LabOrderId == labOrderId, ct);

        if (order == null) return null;

        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TenantId == _tenantContext.TenantId, ct);

        var facilityName = tenant?.TenantName ?? "Medical Laboratory";
        var patient = order.Patient;
        var dob     = patient.DateOfBirth;
        var age     = (int)((DateTime.UtcNow - dob.ToDateTime(TimeOnly.MinValue)).TotalDays / 365.25);

        // Group items by department
        var byDept = order.Items
            .OrderBy(i => i.Department)
            .ThenBy(i => i.TestName)
            .GroupBy(i => i.Department)
            .ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(1.8f, Unit.Centimetre);
                page.MarginVertical(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(9).FontFamily("Arial"));

                page.Header().Column(header =>
                {
                    // Facility name + report title on same row
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Text(facilityName)
                           .FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                        row.ConstantItem(160).AlignRight().Column(col =>
                        {
                            col.Item().Text("LABORATORY RESULT REPORT")
                               .FontSize(10).Bold().FontColor(Colors.Grey.Darken2);
                            col.Item().Text($"Generated: {DateTime.Now:dd-MMM-yyyy HH:mm}")
                               .FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    header.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(Colors.Blue.Darken3);

                    // Patient / order info block
                    header.Item().PaddingTop(6).Background(Colors.Grey.Lighten4).Padding(6).Row(row =>
                    {
                        // Left column: patient demographics
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(60).Text("Patient:").Bold();
                                r.RelativeItem().Text($"{patient.FirstName} {patient.LastName}");
                            });
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(60).Text("MRN:").Bold();
                                r.RelativeItem().Text(patient.MedicalRecordNumber);
                            });
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(60).Text("DOB:").Bold();
                                r.RelativeItem().Text($"{dob:dd-MMM-yyyy}  ({age} yrs)");
                            });
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(60).Text("Gender:").Bold();
                                r.RelativeItem().Text(patient.Gender ?? "—");
                            });
                        });

                        // Right column: order info
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(80).Text("Ordered by:").Bold();
                                r.RelativeItem().Text(
                                    $"Dr. {order.OrderingDoctor.FirstName} {order.OrderingDoctor.LastName}");
                            });
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(80).Text("Order date:").Bold();
                                r.RelativeItem().Text(order.CreatedAt.ToString("dd-MMM-yyyy HH:mm"));
                            });
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(80).Text("Organisation:").Bold();
                                r.RelativeItem().Text(order.Organisation ?? "—");
                            });
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(80).Text("Status:").Bold();
                                r.RelativeItem().Text(order.Status);
                            });
                        });
                    });

                    header.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(8).Column(content =>
                {
                    foreach (var dept in byDept)
                    {
                        // Department heading
                        content.Item().PaddingTop(10)
                            .Background(Colors.Blue.Lighten4)
                            .Padding(4)
                            .Text(dept.Key.ToUpperInvariant())
                            .FontSize(9).Bold().FontColor(Colors.Blue.Darken3);

                        // Results table
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);   // Test name
                                cols.RelativeColumn(2);   // Result
                                cols.RelativeColumn(1.2f);// Unit
                                cols.RelativeColumn(2);   // Ref range
                                cols.ConstantColumn(30);  // Flag
                                cols.RelativeColumn(1.5f);// Status
                            });

                            // Table header
                            static IContainer HeaderCell(IContainer c) =>
                                c.Background(Colors.Grey.Lighten3).Padding(4);

                            table.Header(h =>
                            {
                                h.Cell().Element(HeaderCell).Text("Test").Bold();
                                h.Cell().Element(HeaderCell).Text("Result").Bold();
                                h.Cell().Element(HeaderCell).Text("Unit").Bold();
                                h.Cell().Element(HeaderCell).Text("Ref. Range").Bold();
                                h.Cell().Element(HeaderCell).Text("Flag").Bold();
                                h.Cell().Element(HeaderCell).Text("Status").Bold();
                            });

                            static IContainer DataCell(IContainer c) =>
                                c.BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(4);

                            foreach (var item in dept)
                            {
                                if (item.LabResult?.Observations != null && item.LabResult.Observations.Any())
                                {
                                    // HL7 result — one row per OBX observation
                                    // First row: test name (spanning observations)
                                    var obs = item.LabResult.Observations.OrderBy(o => o.SequenceNumber).ToList();

                                    foreach (var o in obs)
                                    {
                                        var (flagColor, flagText) = FlagStyle(o.AbnormalFlag);

                                        table.Cell().Element(DataCell).PaddingLeft(obs.Count > 1 ? 8 : 0)
                                             .Text(obs.Count > 1 ? $"  {o.TestName}" : item.TestName);
                                        table.Cell().Element(DataCell)
                                             .Text(o.Value ?? "—")
                                             .FontColor(flagColor);
                                        table.Cell().Element(DataCell).Text(o.Units ?? "—");
                                        table.Cell().Element(DataCell).Text(o.ReferenceRange ?? "—");
                                        table.Cell().Element(DataCell)
                                             .Text(flagText).Bold().FontColor(flagColor);
                                        table.Cell().Element(DataCell).Text(item.Status);
                                    }
                                }
                                else
                                {
                                    // Manual result — single row
                                    var (flagColor, flagText) = FlagStyle(item.ManualResultFlag);

                                    table.Cell().Element(DataCell).Text(item.TestName);
                                    table.Cell().Element(DataCell)
                                         .Text(item.ManualResult ?? "Pending")
                                         .FontColor(string.IsNullOrEmpty(item.ManualResult)
                                             ? Colors.Grey.Medium : flagColor);
                                    table.Cell().Element(DataCell).Text(item.ManualResultUnit ?? "—");
                                    table.Cell().Element(DataCell).Text(item.ManualResultReferenceRange ?? "—");
                                    table.Cell().Element(DataCell)
                                         .Text(flagText).Bold().FontColor(flagColor);
                                    table.Cell().Element(DataCell).Text(item.Status);
                                }
                            }
                        });
                    }
                });

                page.Footer().Column(footer =>
                {
                    footer.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                    footer.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Authorized by: _______________________________");
                            col.Item().PaddingTop(2)
                               .Text("This is a computer-generated document. Verify with the issuing laboratory.")
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

    /// <summary>Returns (text color, display text) for a given HL7 abnormal flag.</summary>
    private static (string color, string text) FlagStyle(string? flag) =>
        flag switch
        {
            "H"  or "HH" => (Colors.Red.Medium,    flag!),
            "L"  or "LL" => (Colors.Blue.Medium,   flag!),
            "A"          => (Colors.Orange.Medium,  "A"),
            "N"          => (Colors.Green.Darken2,  "N"),
            _            => (Colors.Black,           ""),
        };
}
