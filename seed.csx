#!/usr/bin/env dotnet-script
#r "nuget: BCrypt.Net-Next, 4.0.3"
#r "nuget: Microsoft.Data.SqlClient, 5.2.2"

using BCrypt.Net;
using Microsoft.Data.SqlClient;

var connStr = "Server=.\\SQLEXPRESS;Database=MediCloudDb;Integrated Security=True;TrustServerCertificate=True;";
var tenantId = Guid.NewGuid();
var userId = Guid.NewGuid();
var adminRoleId = 2; // Admin role
var now = DateTime.UtcNow;
var hash = BCrypt.Net.BCrypt.HashPassword("Admin@1234", 12);

Console.WriteLine($"Seeding tenant + admin user...");
Console.WriteLine($"TenantId: {tenantId}");
Console.WriteLine($"UserId:   {userId}");

using var conn = new SqlConnection(connStr);
conn.Open();

// Insert tenant
using (var cmd = conn.CreateCommand()) {
    cmd.CommandText = @"
        INSERT INTO Tenants (TenantId, TenantCode, TenantName, Subdomain, SubscriptionPlan, IsActive, MaxUsers, StorageQuotaGB, CreatedAt, UpdatedAt)
        VALUES (@id, 'demo', 'Demo Hospital', 'demo', 'Standard', 1, 100, 50, @now, @now)";
    cmd.Parameters.AddWithValue("@id", tenantId);
    cmd.Parameters.AddWithValue("@now", now);
    cmd.ExecuteNonQuery();
}

// Insert admin user
using (var cmd = conn.CreateCommand()) {
    cmd.CommandText = @"
        INSERT INTO Users (UserId, RoleId, TenantId, Email, PasswordHash, FirstName, LastName, IsActive, MustChangePassword, FailedLoginCount, CreatedAt, UpdatedAt)
        VALUES (@id, @roleId, @tenantId, 'admin@demo.com', @hash, 'Admin', 'User', 1, 0, 0, @now, @now)";
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
