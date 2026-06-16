using KayCare.Core.Constants;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KayCare.Infrastructure.Services;

public class RadiologyReportService : IRadiologyReportService
{
    private readonly AppDbContext             _db;
    private readonly IFacilitySettingsService _facility;

    public RadiologyReportService(AppDbContext db, IFacilitySettingsService facility)
    {
        _db       = db;
        _facility = facility;
    }

    public async Task<byte[]?> GenerateReportAsync(Guid radiologyOrderId, CancellationToken ct)
    {
        var order = await _db.RadiologyOrders
            .Include(o => o.Patient)
            .Include(o => o.OrderingDoctor)
            .Include(o => o.Items.OrderBy(i => i.Modality).ThenBy(i => i.ProcedureName))
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.RadiologyOrderId == radiologyOrderId, ct);

        if (order == null) return null;

        var facilitySettings = await _facility.GetAsync(ct);
        var logoBytes        = facilitySettings?.HasLogo == true ? await _facility.GetLogoBytesAsync(ct) : null;
        var facilityName     = facilitySettings?.FacilityName ?? "KayCare HMS";
        var facilityAddress  = facilitySettings?.Address;
        var facilityPhone    = facilitySettings?.Phone;
        var facilityEmail    = facilitySettings?.Email;

        var patient = order.Patient;
        var doctor  = order.OrderingDoctor;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        if (logoBytes != null)
                            row.ConstantItem(70).Image(logoBytes).FitArea();
                        else
                            row.ConstantItem(0);

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(facilityName).FontSize(16).Bold();
                            if (facilityAddress != null) c.Item().Text(facilityAddress).FontSize(9);
                            if (facilityPhone   != null) c.Item().Text($"Tel: {facilityPhone}").FontSize(9);
                            if (facilityEmail   != null) c.Item().Text($"Email: {facilityEmail}").FontSize(9);
                        });

                        row.ConstantItem(110).AlignRight().Column(c =>
                        {
                            c.Item().Text("RADIOLOGY REPORT").FontSize(13).Bold();
                            c.Item().Text(DateTime.UtcNow.ToString("dd MMM yyyy")).FontSize(9);
                        });
                    });
                    col.Item().PaddingTop(4).LineHorizontal(1);
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    // Patient + order info
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("PATIENT INFORMATION").FontSize(9).Bold().FontColor("#555555");
                            c.Item().Text($"{patient.FirstName} {patient.LastName}").Bold();
                            c.Item().Text($"MRN: {patient.MedicalRecordNumber}  |  DOB: {patient.DateOfBirth:dd MMM yyyy}  |  {patient.Gender}");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("ORDER INFORMATION").FontSize(9).Bold().FontColor("#555555");
                            c.Item().Text($"Referred by: Dr. {doctor.FirstName} {doctor.LastName}");
                            c.Item().Text($"Date: {order.CreatedAt:dd MMM yyyy HH:mm}");
                            c.Item().Text($"Priority: {order.Priority}");
                            c.Item().Text($"Status: {order.Status}");
                        });
                    });

                    if (!string.IsNullOrEmpty(order.ClinicalIndication))
                    {
                        col.Item().PaddingTop(8).Column(c =>
                        {
                            c.Item().Text("CLINICAL INDICATION").FontSize(9).Bold().FontColor("#555555");
                            c.Item().Text(order.ClinicalIndication).FontSize(9);
                        });
                    }

                    col.Item().PaddingTop(10).PaddingBottom(4).Text("IMAGING REPORTS").FontSize(11).Bold();

                    foreach (var item in order.Items)
                    {
                        col.Item().PaddingTop(6).Border(0.5f).BorderColor("#cccccc").Padding(10).Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"{item.ProcedureName}  [{item.Modality} — {item.BodyPart}]").Bold().FontSize(10);
                                r.ConstantItem(120).AlignRight().Text(item.AccessionNumber ?? string.Empty).FontSize(9).FontColor("#666666");
                            });

                            c.Item().PaddingTop(2).Text($"Status: {item.Status}").FontSize(9).FontColor("#555555");

                            if (!string.IsNullOrEmpty(item.Findings))
                            {
                                c.Item().PaddingTop(6).Text("Findings:").Bold().FontSize(9);
                                c.Item().Text(item.Findings).FontSize(9);
                            }

                            if (!string.IsNullOrEmpty(item.Impression))
                            {
                                c.Item().PaddingTop(6).Text("Impression:").Bold().FontSize(9);
                                c.Item().Text(item.Impression).FontSize(9);
                            }

                            if (!string.IsNullOrEmpty(item.Recommendations))
                            {
                                c.Item().PaddingTop(6).Text("Recommendations:").Bold().FontSize(9);
                                c.Item().Text(item.Recommendations).FontSize(9);
                            }

                            if (item.SignedAt.HasValue)
                            {
                                c.Item().PaddingTop(8).Text($"Signed: {item.SignedAt.Value:dd MMM yyyy HH:mm} UTC").FontSize(8).FontColor("#888888");
                            }
                        });
                    }

                    if (!string.IsNullOrEmpty(order.Notes))
                    {
                        col.Item().PaddingTop(12).Column(c =>
                        {
                            c.Item().Text("Notes:").Bold().FontSize(9);
                            c.Item().Text(order.Notes).FontSize(9);
                        });
                    }

                    col.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("________________________").FontSize(10);
                            c.Item().Text($"Dr. {doctor.FirstName} {doctor.LastName}").FontSize(9);
                            c.Item().Text("Referring Clinician").FontSize(8).FontColor("#888888");
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text($"Printed: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC").FontSize(8).FontColor("#888888");
                        });
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ").FontSize(8).FontColor("#888888");
                    text.CurrentPageNumber().FontSize(8).FontColor("#888888");
                    text.Span(" of ").FontSize(8).FontColor("#888888");
                    text.TotalPages().FontSize(8).FontColor("#888888");
                });
            });
        });

        return document.GeneratePdf();
    }
}
