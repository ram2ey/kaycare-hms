using KayCare.Core.DTOs.Inpatient;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KayCare.Infrastructure.Services;

public class DischargeSummaryReportService : IDischargeSummaryReportService
{
    private readonly ITenantContext           _tenantContext;
    private readonly IFacilitySettingsService _facility;
    private readonly AppDbContext             _db;

    public DischargeSummaryReportService(
        ITenantContext tenantContext,
        IFacilitySettingsService facility,
        AppDbContext db)
    {
        _tenantContext = tenantContext;
        _facility      = facility;
        _db            = db;
    }

    public async Task<byte[]> GenerateAsync(DischargeSummaryResponse s, CancellationToken ct = default)
    {
        var facilityInfo = await _facility.GetAsync(ct);
        var facilityName = facilityInfo?.FacilityName
            ?? (await _db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.TenantId == _tenantContext.TenantId, ct))?.TenantName
            ?? "Healthcare Facility";
        var logoBytes = await _facility.GetLogoBytesAsync(ct);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(2, Unit.Centimetre);
                page.MarginVertical(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(9).FontFamily("Arial"));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            if (logoBytes != null)
                                col.Item().MaxWidth(70).MaxHeight(35).Image(logoBytes);
                            col.Item().Text(facilityName)
                               .FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                            if (!string.IsNullOrWhiteSpace(facilityInfo?.Address))
                                col.Item().Text(facilityInfo.Address).FontSize(7).FontColor(Colors.Grey.Darken1);
                            if (!string.IsNullOrWhiteSpace(facilityInfo?.Phone))
                                col.Item().Text(facilityInfo.Phone).FontSize(7).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(180).AlignRight().Column(col =>
                        {
                            col.Item().Text("DISCHARGE SUMMARY")
                               .FontSize(13).Bold().FontColor(Colors.Blue.Darken3);
                            col.Item().Text(s.AdmissionNumber).FontSize(9).FontColor(Colors.Grey.Darken1);
                            col.Item().Text($"Generated: {DateTime.Now:dd-MMM-yyyy HH:mm}")
                               .FontSize(7).FontColor(Colors.Grey.Darken1);
                        });
                    });
                    header.Item().PaddingTop(5).LineHorizontal(2).LineColor(Colors.Blue.Darken3);
                });

                page.Content().PaddingTop(10).Column(content =>
                {
                    // Patient info
                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1.5f);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1.5f);
                            cols.RelativeColumn(2);
                        });

                        static IContainer LabelCell(IContainer c) =>
                            c.Background(Colors.Blue.Lighten5).Padding(4);
                        static IContainer ValueCell(IContainer c) =>
                            c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);

                        void Row2(string l1, string v1, string l2, string v2)
                        {
                            table.Cell().Element(LabelCell).Text(l1).SemiBold().FontColor(Colors.Grey.Darken2);
                            table.Cell().Element(ValueCell).Text(v1);
                            table.Cell().Element(LabelCell).Text(l2).SemiBold().FontColor(Colors.Grey.Darken2);
                            table.Cell().Element(ValueCell).Text(v2);
                        }

                        Row2("Patient", s.PatientName, "MRN", s.PatientMrn);
                        Row2("DOB", s.PatientDateOfBirth ?? "—", "Gender", s.PatientGender ?? "—");
                        Row2("Phone", s.PatientPhone ?? "—", "Ward / Bed", $"{s.WardName} · {s.BedNumber}");
                        Row2("Admitted", s.AdmissionDate.ToString("dd-MMM-yyyy"), "Discharged",
                            s.ActualDischargeDate.HasValue ? s.ActualDischargeDate.Value.ToString("dd-MMM-yyyy") : "—");
                        Row2("Length of Stay", s.LengthOfStayDays.HasValue ? $"{s.LengthOfStayDays} day{(s.LengthOfStayDays != 1 ? "s" : "")}" : "—",
                            "Discharge Type", s.DischargeType ?? "—");
                        Row2("Admitting Doctor", s.AdmittingDoctorName, "Condition at Discharge", s.DischargeCondition ?? "—");
                    });

                    // Clinical sections
                    void Section(string title, string? text)
                    {
                        if (string.IsNullOrWhiteSpace(text)) return;
                        content.Item().PaddingTop(10).Column(col =>
                        {
                            col.Item().Background(Colors.Blue.Lighten5).Padding(5)
                               .Text(title).FontSize(9).SemiBold().FontColor(Colors.Blue.Darken3);
                            col.Item().Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                               .Padding(6).Text(text);
                        });
                    }

                    content.Item().PaddingTop(12).Row(row =>
                    {
                        void DiagBlock(string title, string? text, float width)
                        {
                            row.RelativeItem(width).Column(col =>
                            {
                                col.Item().Background(Colors.Blue.Lighten5).Padding(5)
                                   .Text(title).FontSize(9).SemiBold().FontColor(Colors.Blue.Darken3);
                                col.Item().Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                                   .MinHeight(40).Padding(6).Text(text ?? "—");
                            });
                        }
                        DiagBlock("Diagnosis on Admission", s.DiagnosisOnAdmission, 1);
                        row.ConstantItem(10);
                        DiagBlock("Final Diagnosis", s.FinalDiagnosis, 1);
                    });

                    Section("Treatment Summary", s.TreatmentSummary);
                    Section("Procedures Performed", s.ProceduresPerformed);
                    Section("Discharge Medications", s.DischargeMedications);
                    Section("Follow-up Instructions", s.FollowUpInstructions);
                    Section("Attending Physician Notes", s.AttendingPhysicianNotes);
                    if (!string.IsNullOrWhiteSpace(s.DischargeNotes))
                        Section("Discharge Notes", s.DischargeNotes);

                    // Signature block
                    content.Item().PaddingTop(24).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().BorderBottom(1).BorderColor(Colors.Black).Height(1);
                            col.Item().PaddingTop(4).Text("Attending Physician Signature").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                        row.ConstantItem(40);
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().BorderBottom(1).BorderColor(Colors.Black).Height(1);
                            col.Item().PaddingTop(4).Text("Date").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                    });
                });

                page.Footer().Column(footer =>
                {
                    footer.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    footer.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Text("CONFIDENTIAL — For authorised recipients only").FontSize(7).FontColor(Colors.Grey.Medium);
                        row.ConstantItem(60).AlignRight().AlignBottom()
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
