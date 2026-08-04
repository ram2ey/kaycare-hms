using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using KayCare.Infrastructure.Services;

namespace KayCare.Infrastructure.Data;

/// <summary>
/// Design-time factory used exclusively by the EF Core CLI (dotnet ef migrations / database update).
/// Not used at runtime — the real DbContext is resolved from the DI container.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
        {
            var apiPath = Path.Combine(basePath, "src", "KayCare.API");
            if (Directory.Exists(apiPath))
            {
                basePath = apiPath;
            }
            else
            {
                var relativeApiPath = Path.GetFullPath(Path.Combine(basePath, "../KayCare.API"));
                if (Directory.Exists(relativeApiPath))
                {
                    basePath = relativeApiPath;
                }
            }
        }

        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connStr = config.GetConnectionString("DefaultConnection") 
                      ?? config["ConnectionStrings:DefaultConnection"] 
                      ?? config["DATABASE_URL"] 
                      ?? "Host=localhost;Database=kaycare_hms;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connStr,
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        // TenantContext with empty Guid is fine at design time —
        // global query filters are not evaluated during migrations.
        return new AppDbContext(options, new TenantContext());
    }
}
