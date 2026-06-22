using Npgsql;

var connStr = args.Length > 0
    ? args[0]
    : Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
      ?? "Host=localhost;Database=KayCareDb;Username=postgres;Password=postgres;";

Console.WriteLine("Hashing passwords (bcrypt cost 12, takes a few seconds)...");
var hash = BCrypt.Net.BCrypt.HashPassword("Admin@1234", 12);

Console.WriteLine("Connecting to database...");
using var conn = new NpgsqlConnection(connStr);
conn.Open();

var now = DateTime.UtcNow;

var exists = false;
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT COUNT(1) FROM \"Tenants\" WHERE \"TenantCode\" = 'demo'";
    exists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
}

if (!exists)
{
    var tenantId = Guid.NewGuid();
    var userId   = Guid.NewGuid();

    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"
            INSERT INTO ""Tenants"" (""TenantId"", ""TenantCode"", ""TenantName"", ""Subdomain"", ""SubscriptionPlan"", ""IsActive"", ""MaxUsers"", ""StorageQuotaGB"", ""CreatedAt"", ""UpdatedAt"")
            VALUES (@id, 'demo', 'Demo Hospital', 'demo', 'Standard', true, 100, 50, @now, @now)";
        cmd.Parameters.AddWithValue("id",  tenantId);
        cmd.Parameters.AddWithValue("now", now);
        cmd.ExecuteNonQuery();
    }

    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"
            INSERT INTO ""Users"" (""UserId"", ""RoleId"", ""TenantId"", ""Email"", ""PasswordHash"", ""FirstName"", ""LastName"", ""IsActive"", ""MustChangePassword"", ""FailedLoginCount"", ""CreatedAt"", ""UpdatedAt"")
            VALUES (@id, 2, @tenantId, 'admin@demo.com', @hash, 'Admin', 'User', true, false, 0, @now, @now)";
        cmd.Parameters.AddWithValue("id",       userId);
        cmd.Parameters.AddWithValue("tenantId", tenantId);
        cmd.Parameters.AddWithValue("hash",     hash);
        cmd.Parameters.AddWithValue("now",      now);
        cmd.ExecuteNonQuery();
    }

    Console.WriteLine();
    Console.WriteLine("Demo tenant seeded!");
    Console.WriteLine("─────────────────────────────");
    Console.WriteLine("  Email:      admin@demo.com");
    Console.WriteLine("  Password:   Admin@1234");
    Console.WriteLine("  TenantCode: demo");
    Console.WriteLine("─────────────────────────────");
}
else
{
    Console.WriteLine("Demo tenant already exists — skipped.");
}

Console.WriteLine();
Console.WriteLine("Done.");
