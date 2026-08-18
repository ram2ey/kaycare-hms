using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using KayCare.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuestPDF.Infrastructure;


namespace KayCare.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config, IHostEnvironment environment)
    {
        // QuestPDF community license (revenue < $1M USD)
        QuestPDF.Settings.License = LicenseType.Community;

        // Per-request tenant context (populated by TenantResolutionMiddleware)
        services.AddScoped<ITenantContext, TenantContext>();

        // Singleton: the encryption key is fixed process-wide config, not per-request state, and
        // AppDbContext's OnModelCreating captures this instance in its value-converter closures —
        // it must stay the same instance/key for every DbContext instance's model to stay valid.
        services.AddSingleton<IFieldEncryptionService, FieldEncryptionService>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var rawConn = config.GetConnectionString("DefaultConnection")
                          ?? config["ConnectionStrings:DefaultConnection"]
                          ?? config["DATABASE_URL"];
            var connStr = ParseConnectionString(rawConn, environment.IsDevelopment());
            options.UseNpgsql(
                connStr,
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
            );
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenRevocationService, TokenRevocationService>();
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
        services.AddScoped<IPayerTariffService, PayerTariffService>();
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

        // Supabase client — singleton; used by BlobStorageService for file storage
        services.AddSingleton(_ =>
        {
            var url = config["Supabase:Url"]!;
            var key = config["Supabase:ServiceKey"]!;
            var options = new Supabase.SupabaseOptions { AutoConnectRealtime = false };
            var client = new Supabase.Client(url, key, options);
            return client;
        });
        services.AddSingleton<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<ILabResultService, LabResultService>();
        services.AddScoped<ILabOrderService, LabOrderService>();
        services.AddScoped<ILabReportService, LabReportService>();
        services.AddScoped<IRadiologyOrderService, RadiologyOrderService>();
        services.AddScoped<IRadiologyReportService, RadiologyReportService>();
        services.AddScoped<IPrescriptionReportService, PrescriptionReportService>();
        services.AddScoped<IBillingPdfService, BillingPdfService>();
        services.AddScoped<IAuditService, AuditService>();

        // MLLP TCP listener — runs for the lifetime of the application. Opt-out via config
        // (defaults on) because a second open TCP port makes Render's Docker port auto-detection
        // ambiguous and has been observed to time out the deploy outright; Render also only routes
        // one public port anyway, so the listener is unreachable there until a VPN/private tunnel
        // exists. Real on-prem/self-hosted Docker deployments keep today's default-on behavior.
        if (config.GetValue("Mllp:Enabled", true))
        {
            services.AddHostedService<MllpListenerService>();
        }

        return services;
    }

    private static string ParseConnectionString(string? rawConn, bool isDevelopment)
    {
        if (string.IsNullOrWhiteSpace(rawConn)) return string.Empty;

        if (rawConn.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            rawConn.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(rawConn);
                var userInfo = uri.UserInfo.Split(':');
                var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
                var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 5432;
                var database = uri.AbsolutePath.TrimStart('/');

                // Outside Development, require an encrypted connection with real certificate
                // validation — a hosted Postgres (e.g. Render) that ever accepted plaintext or an
                // unvalidated cert would let a network-position attacker read every query in
                // transit, patient data included. Local dev Postgres typically isn't configured
                // for SSL at all, so keep the previous permissive behavior there.
                var sslClause = isDevelopment
                    ? "SSL Mode=Prefer;Trust Server Certificate=true;"
                    : "SSL Mode=Require;Trust Server Certificate=false;";

                return $"Host={host};Port={port};Database={database};Username={username};Password={password};{sslClause}";
            }
            catch
            {
                return rawConn;
            }
        }

        return rawConn;
    }
}
