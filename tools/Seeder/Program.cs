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

// 1. Ensure Tenant exists
Guid tenantId;
var tenantExists = false;
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT \"TenantId\" FROM \"Tenants\" WHERE \"TenantCode\" = 'demo'";
    var result = cmd.ExecuteScalar();
    if (result != null)
    {
        tenantId = (Guid)result;
        tenantExists = true;
    }
    else
    {
        tenantId = Guid.NewGuid();
    }
}

if (!tenantExists)
{
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"
            INSERT INTO ""Tenants"" (""TenantId"", ""TenantCode"", ""TenantName"", ""Subdomain"", ""SubscriptionPlan"", ""IsActive"", ""MaxUsers"", ""StorageQuotaGB"", ""CreatedAt"", ""UpdatedAt"")
            VALUES (@id, 'demo', 'Demo Hospital', 'demo', 'Standard', true, 100, 50, @now, @now)";
        cmd.Parameters.AddWithValue("id",  tenantId);
        cmd.Parameters.AddWithValue("now", now);
        cmd.ExecuteNonQuery();
    }
    Console.WriteLine("Demo tenant created.");
}

// 2. Define users to seed
var usersToSeed = new (string Email, int RoleId, string FirstName, string LastName)[]
{
    ("admin@demo.com", 2, "Admin", "User"),
    ("doctor@demo.com", 3, "Doctor", "User"),
    ("nurse@demo.com", 4, "Nurse", "User"),
    ("receptionist@demo.com", 5, "Receptionist", "User"),
    ("pharmacist@demo.com", 6, "Pharmacist", "User"),
    ("labtech@demo.com", 7, "LabTech", "User"),
    ("biller@demo.com", 8, "Biller", "User")
};

// 3. Seed users
foreach (var u in usersToSeed)
{
    var userExists = false;
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "SELECT COUNT(1) FROM \"Users\" WHERE \"Email\" = @email";
        cmd.Parameters.AddWithValue("email", u.Email);
        userExists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    if (!userExists)
    {
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT INTO ""Users"" (""UserId"", ""RoleId"", ""TenantId"", ""Email"", ""PasswordHash"", ""FirstName"", ""LastName"", ""IsActive"", ""MustChangePassword"", ""FailedLoginCount"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@id, @roleId, @tenantId, @email, @hash, @firstName, @lastName, true, false, 0, @now, @now)";
            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("roleId", u.RoleId);
            cmd.Parameters.AddWithValue("tenantId", tenantId);
            cmd.Parameters.AddWithValue("email", u.Email);
            cmd.Parameters.AddWithValue("hash", hash);
            cmd.Parameters.AddWithValue("firstName", u.FirstName);
            cmd.Parameters.AddWithValue("lastName", u.LastName);
            cmd.Parameters.AddWithValue("now", now);
            cmd.ExecuteNonQuery();
        }
        Console.WriteLine($"Seeded user: {u.Email} (Role: {u.RoleId})");
    }
    else
    {
        Console.WriteLine($"User already exists: {u.Email}");
    }
}

Console.WriteLine();
Console.WriteLine("Demo users seeded successfully!");
Console.WriteLine("─────────────────────────────");
Console.WriteLine("  Password:   Admin@1234");
Console.WriteLine("  TenantCode: demo");
Console.WriteLine("─────────────────────────────");
Console.WriteLine("Done.");
