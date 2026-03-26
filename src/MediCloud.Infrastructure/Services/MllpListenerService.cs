using System.Net;
using System.Net.Sockets;
using System.Text;
using MediCloud.Core.Constants;
using MediCloud.Core.Entities;
using MediCloud.Core.Interfaces;
using MediCloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MediCloud.Infrastructure.Services;

/// <summary>
/// MLLP (Minimal Lower Layer Protocol) TCP listener on port 2575.
/// Receives HL7 ORU^R01 messages from lab equipment via Mirth Connect,
/// persists LabResult + LabObservation rows, and logs a notification
/// to the ordering doctor.
/// </summary>
public sealed class MllpListenerService : BackgroundService
{
    private const int  MllpPort = 2575;
    private const byte VT = 0x0B;   // vertical tab — MLLP start-of-block
    private const byte FS = 0x1C;   // file separator — MLLP end-of-block
    private const byte CR = 0x0D;   // carriage return — follows FS

    private readonly ILogger<MllpListenerService> _logger;
    private readonly IServiceScopeFactory         _scopeFactory;

    public MllpListenerService(
        ILogger<MllpListenerService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger       = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, MllpPort);
        listener.Start();
        _logger.LogInformation("MLLP listener started on TCP port {Port}", MllpPort);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MLLP: error accepting connection");
                    continue;
                }

                // Fire-and-forget — each connection handled independently
                _ = HandleClientAsync(client, stoppingToken);
            }
        }
        finally
        {
            listener.Stop();
            _logger.LogInformation("MLLP listener stopped");
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using var _ = client;
        var remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        try
        {
            var stream = client.GetStream();
            var raw    = await ReadMllpMessageAsync(stream, ct);
            if (raw == null)
            {
                _logger.LogWarning("MLLP: empty or malformed message from {Remote}", remote);
                return;
            }

            var ack = await ProcessMessageAsync(raw, ct);
            await WriteMllpAckAsync(stream, ack, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MLLP: unhandled error for client {Remote}", remote);
        }
    }

    // ── MLLP framing ─────────────────────────────────────────────────────────

    private static async Task<string?> ReadMllpMessageAsync(NetworkStream stream, CancellationToken ct)
    {
        var buf = new byte[1];

        // Scan for VT start-of-block
        while (true)
        {
            if (await stream.ReadAsync(buf, ct) == 0) return null;
            if (buf[0] == VT) break;
        }

        // Accumulate bytes until FS end-of-block
        var message = new List<byte>(4096);
        while (true)
        {
            if (await stream.ReadAsync(buf, ct) == 0) break;
            if (buf[0] == FS)
            {
                await stream.ReadAsync(buf, ct); // consume trailing CR
                break;
            }
            message.Add(buf[0]);
        }

        return message.Count == 0 ? null : Encoding.ASCII.GetString(message.ToArray());
    }

    private static async Task WriteMllpAckAsync(NetworkStream stream, string ack, CancellationToken ct)
    {
        var payload = new List<byte> { VT };
        payload.AddRange(Encoding.ASCII.GetBytes(ack));
        payload.Add(FS);
        payload.Add(CR);
        await stream.WriteAsync(payload.ToArray(), ct);
        await stream.FlushAsync(ct);
    }

    // ── Message processing ────────────────────────────────────────────────────

    private async Task<string> ProcessMessageAsync(string rawMessage, CancellationToken ct)
    {
        ParsedOruR01? parsed;
        try
        {
            parsed = Hl7Parser.ParseOruR01(rawMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MLLP: HL7 parse failure");
            return BuildAck("AE", "Unknown", "Parse error: " + ex.Message);
        }

        if (parsed == null)
        {
            _logger.LogWarning("MLLP: received non-ORU^R01 message — ignored");
            return BuildAck("AA", "Unknown", "Not ORU^R01 — ignored");
        }

        using var scope          = _scopeFactory.CreateScope();
        var db                   = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tenantContext         = scope.ServiceProvider.GetRequiredService<ITenantContext>();

        // ── Resolve tenant from MSH-4 (sending facility = TenantCode) ────────
        var tenant = await db.Tenants
            .FirstOrDefaultAsync(t => t.TenantCode == parsed.SendingFacility, ct);

        if (tenant == null)
        {
            _logger.LogWarning("MLLP: unknown sending facility '{Facility}'", parsed.SendingFacility);
            return BuildAck("AE", parsed.MessageControlId,
                $"Unknown facility: {parsed.SendingFacility}");
        }

        // Set tenant context so global query filters apply correctly
        tenantContext.TenantId   = tenant.TenantId;
        tenantContext.TenantCode = tenant.TenantCode;

        // ── Duplicate guard ───────────────────────────────────────────────────
        var duplicate = await db.LabResults
            .AnyAsync(r => r.AccessionNumber == parsed.AccessionNumber, ct);

        if (duplicate)
        {
            _logger.LogWarning("MLLP: duplicate accession '{Accession}' — ACK and skip",
                parsed.AccessionNumber);
            return BuildAck("AA", parsed.MessageControlId,
                "Duplicate accession — already processed");
        }

        // ── Patient lookup ───────────────────────────────────────────────────
        var patient = await db.Patients
            .FirstOrDefaultAsync(p => p.MedicalRecordNumber == parsed.PatientMrn, ct);

        if (patient == null)
        {
            _logger.LogWarning("MLLP: patient not found for MRN '{MRN}'", parsed.PatientMrn);
            return BuildAck("AE", parsed.MessageControlId,
                $"Patient not found: {parsed.PatientMrn}");
        }

        // ── Ordering doctor lookup (OBR-16 component 1 = internal UserId) ────
        User? doctor = null;
        if (Guid.TryParse(parsed.OrderingDoctorId, out var doctorId))
            doctor = await db.Users.FirstOrDefaultAsync(u => u.UserId == doctorId, ct);

        // ── Match to pending LabOrderItem (if order was placed in system) ─────
        var orderItem = await db.LabOrderItems
            .Include(i => i.LabOrder)
                .ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(i => i.AccessionNumber == parsed.AccessionNumber, ct);

        // ── Persist LabResult ─────────────────────────────────────────────────
        var labResult = new LabResult
        {
            PatientId            = patient.PatientId,
            OrderingDoctorUserId = doctor?.UserId,
            AccessionNumber      = parsed.AccessionNumber,
            OrderCode            = parsed.OrderCode,
            OrderName            = parsed.OrderName,
            OrderedAt            = parsed.OrderedAt,
            ReceivedAt           = parsed.ResultsDateTime ?? DateTime.UtcNow,
            Status               = LabResultStatus.Received,
            RawHl7               = rawMessage,
            LabOrderItemId       = orderItem?.LabOrderItemId,
        };

        db.LabResults.Add(labResult);
        await db.SaveChangesAsync(ct); // LabResultId populated after save

        // ── Update LabOrderItem and parent order status ───────────────────────
        if (orderItem != null)
        {
            orderItem.Status      = Core.Constants.LabOrderItemStatus.Resulted;
            orderItem.ResultedAt  = DateTime.UtcNow;
            orderItem.LabResultId = labResult.LabResultId;

            var siblings = orderItem.LabOrder.Items.ToList();
            var allDone  = siblings.All(i => i.LabOrderItemId == orderItem.LabOrderItemId
                                          || i.Status is Core.Constants.LabOrderItemStatus.Resulted
                                                      or Core.Constants.LabOrderItemStatus.Signed);
            var anyDone  = siblings.Any(i => i.LabOrderItemId == orderItem.LabOrderItemId
                                          || i.Status is Core.Constants.LabOrderItemStatus.Resulted
                                                      or Core.Constants.LabOrderItemStatus.Signed);

            orderItem.LabOrder.Status = allDone ? Core.Constants.LabOrderStatus.Completed
                                      : anyDone ? Core.Constants.LabOrderStatus.PartiallyCompleted
                                                : Core.Constants.LabOrderStatus.Active;
        }

        // ── Persist LabObservations ───────────────────────────────────────────
        for (var i = 0; i < parsed.Observations.Count; i++)
        {
            var obs = parsed.Observations[i];
            db.LabObservations.Add(new LabObservation
            {
                LabResultId    = labResult.LabResultId,
                TenantId       = tenant.TenantId,
                SequenceNumber = obs.SequenceNumber > 0 ? obs.SequenceNumber : i + 1,
                TestCode       = obs.TestCode,
                TestName       = obs.TestName,
                Value          = obs.Value,
                Units          = obs.Units,
                ReferenceRange = obs.ReferenceRange,
                AbnormalFlag   = obs.AbnormalFlag,
            });
        }

        await db.SaveChangesAsync(ct);

        // ── Doctor notification (structured log — extend to email/push later) ─
        if (doctor != null)
        {
            _logger.LogInformation(
                "NOTIFICATION: Lab result {AccessionNumber} ({OrderName}) received for " +
                "Dr. {FirstName} {LastName} (UserId={DoctorId}), Patient MRN={MRN}, " +
                "{ObsCount} observation(s)",
                labResult.AccessionNumber, labResult.OrderName,
                doctor.FirstName, doctor.LastName, doctor.UserId,
                patient.MedicalRecordNumber, parsed.Observations.Count);
        }
        else
        {
            _logger.LogInformation(
                "Lab result {AccessionNumber} received for Patient MRN={MRN}, " +
                "{ObsCount} observation(s) — ordering doctor not resolved",
                labResult.AccessionNumber, patient.MedicalRecordNumber,
                parsed.Observations.Count);
        }

        return BuildAck("AA", parsed.MessageControlId, "Accepted");
    }

    // ── ACK builder ───────────────────────────────────────────────────────────

    private static string BuildAck(string ackCode, string messageControlId, string text)
    {
        var ts = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        // HL7 ACK; \r is the segment terminator
        return $"MSH|^~\\&|MediCloud|EMR|Lab|System|{ts}||ACK|ACK{ts}|P|2.5\r" +
               $"MSA|{ackCode}|{messageControlId}|{text}\r";
    }
}
