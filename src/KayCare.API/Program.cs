using System.Text;
using KayCare.API.Auth;
using KayCare.Core.Constants;
using KayCare.Core.Exceptions;
using KayCare.Infrastructure;
using KayCare.Infrastructure.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Disable FileSystemWatcher in container environments to prevent inotify limit exhaustion
builder.Host.ConfigureAppConfiguration((_, configBuilder) =>
{
    foreach (var source in configBuilder.Sources)
    {
        if (source is Microsoft.Extensions.Configuration.FileConfigurationSource fileSource)
        {
            fileSource.ReloadOnChange = false;
        }
    }
});

// ── Infrastructure (DbContext, services, tenant context) ──────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();

// ── Forwarded headers (Render terminates TLS at its edge and forwards plain HTTP) ─────────────
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Render's edge isn't in .NET's default trusted-proxy list — clear so the header is honored.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ── JWT Authentication (also accepts the token from an httpOnly cookie) ───────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateIssuer   = true,
            ValidIssuer      = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience    = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew        = TimeSpan.Zero   // HIPAA: no clock skew tolerance
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Cookie takes priority; falls through to the default Authorization: Bearer
                // header extraction if absent (keeps Swagger/Postman/tooling working, and keeps
                // an already-loaded old frontend build working against this backend mid-deploy).
                if (context.Request.Cookies.TryGetValue(AuthCookieNames.Token, out var token) &&
                    !string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ── CSRF (double-submit cookie via ASP.NET Core's built-in antiforgery) ───────────────────────
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName        = "X-XSRF-TOKEN";   // must be set explicitly — default is null
    options.Cookie.Name       = "XSRF-TOKEN-C";   // the *ambient* half only; the JS-readable
                                                    // request token is delivered via JSON body
                                                    // instead (see AuthController), since a
                                                    // cross-site cookie set by this API is never
                                                    // readable by the frontend's own JS anyway
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = builder.Environment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None;
});

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin =>
                allowedOrigins.Contains(origin) ||
                origin.StartsWith("http://localhost:"))
              // Dropped the ".onrender.com" wildcard (finding L1) — now that AllowCredentials()
              // is set below, that wildcard would let any other onrender.com-hosted site send
              // cookie-authenticated requests to this API. Only the explicit allow-list +
              // localhost dev convenience remain.
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ── Swagger with Bearer token support ────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "KayCare HMS API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter: Bearer {token}",
        Name        = "Authorization",
        In          = ParameterLocation.Header,
        Type        = SecuritySchemeType.ApiKey,
        Scheme      = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            []
        }
    });
});

var app = builder.Build();

// ── Fail-fast config checks ────────────────────────────────────────────────────
// These secrets default to a documented placeholder in appsettings.json; refuse to start
// rather than silently running with a publicly-known key.
static void RequireRealSecret(string configKey, string? value)
{
    if (string.IsNullOrWhiteSpace(value) || value == "PLACEHOLDER-change-in-production")
    {
        throw new InvalidOperationException(
            $"{configKey} is missing or is still the placeholder value. Set a real secret before starting.");
    }
}
RequireRealSecret("Hl7:WebhookApiKey", app.Configuration["Hl7:WebhookApiKey"]);
RequireRealSecret("Hl7:MllpSharedSecret", app.Configuration["Hl7:MllpSharedSecret"]);

// ── Global exception handler ──────────────────────────────────────────────────
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var ex = feature?.Error;
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("GlobalExceptionHandler");
        var traceId = context.TraceIdentifier;

        logger.LogError(ex, "Unhandled exception. TraceId={TraceId} Path={Path}", traceId, feature?.Path);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = ex switch
        {
            AppException appEx => appEx.StatusCode,
            _                  => 500
        };

        // AppException messages are the app's own well-formed, user-facing errors — always safe
        // to return. Anything else (framework/DB/etc. exceptions) may contain internal details
        // (table names, query fragments) and is only shown in Development.
        var message = ex switch
        {
            AppException appEx => appEx.Message,
            _ when app.Environment.IsDevelopment() => ex?.Message ?? "An unexpected error occurred.",
            _ => "An unexpected error occurred."
        };

        await context.Response.WriteAsJsonAsync(new { error = message, traceId });
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Render's health check hits this — must stay reachable without auth and without Swagger.
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();

// CSRF only needs to know whether the request is cookie-authenticated (HttpContext.User +
// cookie presence) — no dependency on tenant resolution or must-change-password state, so it
// runs as early as possible after authentication to reject a forged request before any
// tenant-resolution or business-rule work happens.
app.UseMiddleware<CsrfProtectionMiddleware>();

// Tenant must be resolved after authentication so TenantId is in scope from JWT if header is missing
app.UseMiddleware<TenantResolutionMiddleware>();

// Must run after tenant resolution (needs the authenticated claims already validated) and before
// authorization, so a user who must change their password can't use any other endpoint first.
app.UseMiddleware<MustChangePasswordMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Migrations must always run. Demo-tenant/account seeding is opt-in only (local dev by default,
// or a deliberately-configured demo/sales Production instance) — never unconditional in Production.
await KayCare.Infrastructure.Data.DbInitializer.MigrateAsync(app.Services, app.Logger);

var enableDemoSeed = app.Environment.IsDevelopment()
    || app.Configuration.GetValue<bool>("Seeding:EnableDemoData");
if (enableDemoSeed)
{
    await KayCare.Infrastructure.Data.DbInitializer.SeedDemoDataAsync(app.Services, app.Logger);
}

app.Run();

// Exposed for WebApplicationFactory in integration tests
public partial class Program { }
