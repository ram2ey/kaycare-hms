using Azure.Storage.Blobs;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using KayCare.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;


namespace KayCare.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // QuestPDF community license (revenue < $1M USD)
        QuestPDF.Settings.License = LicenseType.Community;

        // Per-request tenant context (populated by TenantResolutionMiddleware)
        services.AddScoped<ITenantContext, TenantContext>();

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                config.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
            )
        );

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IConsultationService, ConsultationService>();
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        services.AddScoped<IPrescriptionTemplateService, PrescriptionTemplateService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IBillingReportsService, BillingReportsService>();
        services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
        services.AddScoped<IPayerService, PayerService>();
        services.AddScoped<IChargeCaptureService, ChargeCaptureService>();
        services.AddScoped<IInsuranceClaimService, InsuranceClaimService>();
        services.AddScoped<ICreditNoteService, CreditNoteService>();
        services.AddScoped<IRefundService, RefundService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IFacilitySettingsService, FacilitySettingsService>();
        services.AddScoped<IBillTemplateService, BillTemplateService>();
        services.AddScoped<IDrugInventoryService, DrugInventoryService>();
        services.AddScoped<IStockMovementService, StockMovementService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<ICSRegisterService, CSRegisterService>();
        services.AddScoped<ICSRegisterReportService, CSRegisterReportService>();
        services.AddScoped<IWardService, WardService>();
        services.AddScoped<IAdmissionService, AdmissionService>();
        services.AddScoped<IInpatientBillingService, InpatientBillingService>();
        services.AddScoped<IDischargeSummaryReportService, DischargeSummaryReportService>();
        services.AddScoped<IVitalSignsService, VitalSignsService>();
        services.AddScoped<INursingNoteService, NursingNoteService>();
        services.AddScoped<IMedicationAdministrationService, MedicationAdministrationService>();
        services.AddScoped<IReferralService, ReferralService>();
        services.AddScoped<IReportsService, ReportsService>();
        services.AddScoped<IIcdCodeService, IcdCodeService>();

        // Azure Blob Storage — singleton client; per-request scoped service
        services.AddSingleton(_ =>
            new BlobServiceClient(config["BlobStorage:ConnectionString"]));
        services.AddSingleton<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<ILabResultService, LabResultService>();
        services.AddScoped<ILabOrderService, LabOrderService>();
        services.AddScoped<ILabReportService, LabReportService>();
        services.AddScoped<IPrescriptionReportService, PrescriptionReportService>();
        services.AddScoped<IBillingPdfService, BillingPdfService>();
        services.AddScoped<IAuditService, AuditService>();

        // MLLP TCP listener — runs for the lifetime of the application
        services.AddHostedService<MllpListenerService>();

        return services;
    }
}
