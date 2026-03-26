namespace MediCloud.Tests.Infrastructure;

/// <summary>Snapshot of a tenant and its seeded users, created once per test run.</summary>
public record TestTenant(
    Guid   TenantId,
    string TenantCode,
    Guid   AdminUserId,
    string AdminEmail,
    Guid   DoctorUserId,
    string DoctorEmail
);
