#!/usr/bin/env dotnet-script
#r "nuget: BCrypt.Net-Next, 4.0.3"
#r "nuget: Npgsql, 8.0.3"

using BCrypt.Net;
using Npgsql;

var connStr = Environment.GetEnvironmentVariable("DB_CONNECTION")
    ?? "Host=localhost;Database=KayCareDb;Username=postgres;Password=postgres;";
var tenantId = Guid.NewGuid();
var userId = Guid.NewGuid();
var adminRoleId = 2; // Admin role
var now = DateTime.UtcNow;
var hash = BCrypt.Net.BCrypt.HashPassword("Admin@1234", 12);

Console.WriteLine($"Seeding PostgreSQL tenant + admin user...");
Console.WriteLine($"Connection: {connStr}");
Console.WriteLine($"TenantId:   {tenantId}");
Console.WriteLine($"UserId:     {userId}");

using var conn = new NpgsqlConnection(connStr);
conn.Open();

// Insert tenant
using (var cmd = conn.CreateCommand()) {
    cmd.CommandText = @"
        INSERT INTO ""Tenants"" (""TenantId"", ""TenantCode"", ""TenantName"", ""Subdomain"", ""SubscriptionPlan"", ""IsActive"", ""MaxUsers"", ""StorageQuotaGB"", ""CreatedAt"", ""UpdatedAt"")
        VALUES (@id, 'demo', 'Demo Hospital', 'demo', 'Standard', true, 100, 50, @now, @now)
        ON CONFLICT (""TenantCode"") DO NOTHING;";
    cmd.Parameters.AddWithValue("@id", tenantId);
    cmd.Parameters.AddWithValue("@now", now);
    cmd.ExecuteNonQuery();
}

// Insert admin user
using (var cmd = conn.CreateCommand()) {
    cmd.CommandText = @"
        INSERT INTO ""Users"" (""UserId"", ""RoleId"", ""TenantId"", ""Email"", ""PasswordHash"", ""FirstName"", ""LastName"", ""IsActive"", ""MustChangePassword"", ""FailedLoginCount"", ""CreatedAt"", ""UpdatedAt"")
        VALUES (@id, @roleId, @tenantId, 'admin@demo.com', @hash, 'Admin', 'User', true, false, 0, @now, @now)
        ON CONFLICT (""Email"") DO NOTHING;";
    cmd.Parameters.AddWithValue("@id", userId);
    cmd.Parameters.AddWithValue("@roleId", adminRoleId);
    cmd.Parameters.AddWithValue("@tenantId", tenantId);
    cmd.Parameters.AddWithValue("@hash", hash);
    cmd.Parameters.AddWithValue("@now", now);
    cmd.ExecuteNonQuery();
}

Console.WriteLine("Done!");
Console.WriteLine();
Console.WriteLine("Login credentials:");
Console.WriteLine("  Email:      admin@demo.com");
Console.WriteLine("  Password:   Admin@1234");
Console.WriteLine("  TenantCode: demo");
Console.WriteLine();
Console.WriteLine("Use X-Tenant-Code: demo header (set automatically via login response)");
