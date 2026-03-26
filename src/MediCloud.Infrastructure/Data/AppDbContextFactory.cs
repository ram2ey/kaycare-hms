using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using MediCloud.Infrastructure.Services;

namespace MediCloud.Infrastructure.Data;

/// <summary>
/// Design-time factory used exclusively by the EF Core CLI (dotnet ef migrations / database update).
/// Not used at runtime — the real DbContext is resolved from the DI container.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../MediCloud.API"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(config.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        // TenantContext with empty Guid is fine at design time —
        // global query filters are not evaluated during migrations.
        return new AppDbContext(options, new TenantContext());
    }
}
