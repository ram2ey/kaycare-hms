using Microsoft.Data.SqlClient;

var connStr = args.Length > 0
    ? args[0]
    : "Server=.\\SQLEXPRESS;Database=KayCareDb;Integrated Security=True;TrustServerCertificate=True;";

Console.WriteLine("Hashing passwords (bcrypt cost 12, takes a few seconds)...");
var hash = BCrypt.Net.BCrypt.HashPassword("Admin@1234", 12);

Console.WriteLine("Connecting to database...");
using var conn = new SqlConnection(connStr);
conn.Open();

var now = DateTime.UtcNow;

// ── HMS demo tenant ───────────────────────────────────────────────────────────

var hmsExists = false;
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT COUNT(1) FROM Tenants WHERE TenantCode = 'demo'";
    hmsExists = (int)cmd.ExecuteScalar()! > 0;
}

if (!hmsExists)
{
    var tenantId = Guid.NewGuid();
    var userId   = Guid.NewGuid();

    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"
            INSERT INTO Tenants (TenantId, TenantCode, TenantName, Subdomain, TenantType, SubscriptionPlan, IsActive, MaxUsers, StorageQuotaGB, CreatedAt, UpdatedAt)
            VALUES (@id, 'demo', 'Demo Hospital', 'demo', 'HMS', 'Standard', 1, 100, 50, @now, @now)";
        cmd.Parameters.AddWithValue("@id", tenantId);
        cmd.Parameters.AddWithValue("@now", now);
        cmd.ExecuteNonQuery();
    }

    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"
            INSERT INTO Users (UserId, RoleId, TenantId, Email, PasswordHash, FirstName, LastName, IsActive, MustChangePassword, FailedLoginCount, CreatedAt, UpdatedAt)
            VALUES (@id, 2, @tenantId, 'admin@demo.com', @hash, 'Admin', 'User', 1, 0, 0, @now, @now)";
        cmd.Parameters.AddWithValue("@id",       userId);
        cmd.Parameters.AddWithValue("@tenantId", tenantId);
        cmd.Parameters.AddWithValue("@hash",     hash);
        cmd.Parameters.AddWithValue("@now",      now);
        cmd.ExecuteNonQuery();
    }

    Console.WriteLine();
    Console.WriteLine("HMS demo tenant seeded!");
    Console.WriteLine("─────────────────────────────");
    Console.WriteLine("  Email:      admin@demo.com");
    Console.WriteLine("  Password:   Admin@1234");
    Console.WriteLine("  TenantCode: demo");
    Console.WriteLine("─────────────────────────────");
}
else
{
    // Ensure TenantType is set on existing tenant
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "UPDATE Tenants SET TenantType = 'HMS' WHERE TenantCode = 'demo' AND (TenantType IS NULL OR TenantType = '')";
    cmd.ExecuteNonQuery();
    Console.WriteLine("HMS demo tenant already exists — skipped.");
}

// ── PharmOS demo tenant ───────────────────────────────────────────────────────

var pharmosTenantExists = false;
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT COUNT(1) FROM Tenants WHERE TenantCode = 'pharmdemo'";
    pharmosTenantExists = (int)cmd.ExecuteScalar()! > 0;
}

if (!pharmosTenantExists)
{
    var tenantId = Guid.NewGuid();
    var userId   = Guid.NewGuid();

    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"
            INSERT INTO Tenants (TenantId, TenantCode, TenantName, Subdomain, TenantType, SubscriptionPlan, IsActive, MaxUsers, StorageQuotaGB, CreatedAt, UpdatedAt)
            VALUES (@id, 'pharmdemo', 'Demo Pharmacy', 'pharmdemo', 'PharmOS', 'Standard', 1, 20, 5, @now, @now)";
        cmd.Parameters.AddWithValue("@id",  tenantId);
        cmd.Parameters.AddWithValue("@now", now);
        cmd.ExecuteNonQuery();
    }

    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"
            INSERT INTO Users (UserId, RoleId, TenantId, Email, PasswordHash, FirstName, LastName, IsActive, MustChangePassword, FailedLoginCount, CreatedAt, UpdatedAt)
            VALUES (@id, 6, @tenantId, 'admin@pharmdemo.com', @hash, 'Admin', 'Pharmacist', 1, 0, 0, @now, @now)";
        cmd.Parameters.AddWithValue("@id",       userId);
        cmd.Parameters.AddWithValue("@tenantId", tenantId);
        cmd.Parameters.AddWithValue("@hash",     hash);
        cmd.Parameters.AddWithValue("@now",      now);
        cmd.ExecuteNonQuery();
    }

    Console.WriteLine();
    Console.WriteLine("PharmOS demo tenant seeded!");
    Console.WriteLine("─────────────────────────────");
    Console.WriteLine("  Email:      admin@pharmdemo.com");
    Console.WriteLine("  Password:   Admin@1234");
    Console.WriteLine("  TenantCode: pharmdemo");
    Console.WriteLine("─────────────────────────────");
}
else
{
    Console.WriteLine("PharmOS demo tenant already exists — skipped.");
}

Console.WriteLine();
Console.WriteLine("Done.");
