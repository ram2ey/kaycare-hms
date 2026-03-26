using MediCloud.Core.Entities;
using MediCloud.Core.Interfaces;
using MediCloud.Infrastructure.Data;
using Microsoft.AspNetCore.Http;

namespace MediCloud.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext        _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _http;

    public AuditService(AppDbContext db, ICurrentUserService currentUser, IHttpContextAccessor http)
    {
        _db          = db;
        _currentUser = currentUser;
        _http        = http;
    }

    public async Task LogAsync(
        string  action,
        string  entityType,
        Guid    entityId,
        Guid?   patientId,
        string? details     = null,
        CancellationToken ct = default)
    {
        var log = new AuditLog
        {
            TenantId   = _currentUser.TenantId,
            UserId     = _currentUser.UserId,
            UserEmail  = _currentUser.Email,
            Action     = action,
            EntityType = entityType,
            EntityId   = entityId,
            PatientId  = patientId,
            Details    = details,
            IpAddress  = _http.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            Timestamp  = DateTime.UtcNow,
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}
