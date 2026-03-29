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
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IConsultationService, ConsultationService>();
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        services.AddScoped<IPrescriptionTemplateService, PrescriptionTemplateService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
        services.AddScoped<IPayerService, PayerService>();
        services.AddScoped<IChargeCaptureService, ChargeCaptureService>();

        // Azure Blob Storage — singleton client; per-request scoped service
        services.AddSingleton(_ =>
            new BlobServiceClient(config["BlobStorage:ConnectionString"]));
        services.AddSingleton<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<ILabResultService, LabResultService>();
        services.AddScoped<ILabOrderService, LabOrderService>();
        services.AddScoped<ILabReportService, LabReportService>();
        services.AddScoped<IPrescriptionReportService, PrescriptionReportService>();
        services.AddScoped<IBillingReportService, BillingReportService>();
        services.AddScoped<IAuditService, AuditService>();

        // MLLP TCP listener — runs for the lifetime of the application
        services.AddHostedService<MllpListenerService>();

        return services;
    }
}
