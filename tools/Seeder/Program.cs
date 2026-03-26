using Microsoft.Data.SqlClient;

var connStr = "Server=.\\SQLEXPRESS;Database=MediCloudDb;Integrated Security=True;TrustServerCertificate=True;";
var tenantId = Guid.NewGuid();
var userId = Guid.NewGuid();
var now = DateTime.UtcNow;

Console.WriteLine("Hashing password (bcrypt cost 12, takes a few seconds)...");
var hash = BCrypt.Net.BCrypt.HashPassword("Admin@1234", 12);

Console.WriteLine("Connecting to MediCloudDb...");
using var conn = new SqlConnection(connStr);
conn.Open();

// Insert tenant
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = @"
        INSERT INTO Tenants (TenantId, TenantCode, TenantName, Subdomain, SubscriptionPlan, IsActive, MaxUsers, StorageQuotaGB, CreatedAt, UpdatedAt)
        VALUES (@id, 'demo', 'Demo Hospital', 'demo', 'Standard', 1, 100, 50, @now, @now)";
    cmd.Parameters.AddWithValue("@id", tenantId);
    cmd.Parameters.AddWithValue("@now", now);
    cmd.ExecuteNonQuery();
}

// Insert admin user (RoleId 2 = Admin)
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = @"
        INSERT INTO Users (UserId, RoleId, TenantId, Email, PasswordHash, FirstName, LastName, IsActive, MustChangePassword, FailedLoginCount, CreatedAt, UpdatedAt)
        VALUES (@id, 2, @tenantId, 'admin@demo.com', @hash, 'Admin', 'User', 1, 0, 0, @now, @now)";
    cmd.Parameters.AddWithValue("@id", userId);
    cmd.Parameters.AddWithValue("@tenantId", tenantId);
    cmd.Parameters.AddWithValue("@hash", hash);
    cmd.Parameters.AddWithValue("@now", now);
    cmd.ExecuteNonQuery();
}

Console.WriteLine();
Console.WriteLine("Seed complete!");
Console.WriteLine("─────────────────────────────");
Console.WriteLine("  Email:      admin@demo.com");
Console.WriteLine("  Password:   Admin@1234");
Console.WriteLine("  TenantCode: demo");
Console.WriteLine("─────────────────────────────");
