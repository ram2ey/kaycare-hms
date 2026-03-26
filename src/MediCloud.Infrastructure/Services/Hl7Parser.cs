namespace MediCloud.Infrastructure.Services;

/// <summary>
/// Minimal HL7 v2.x parser focused on ORU^R01 messages received from lab equipment
/// via Mirth Connect middleware.
/// </summary>
public static class Hl7Parser
{
    public static ParsedOruR01? ParseOruR01(string rawMessage)
    {
        // Normalise line endings — HL7 uses \r; TCP streams may add \n
        var segments = rawMessage
            .Replace("\r\n", "\r")
            .Replace("\n", "\r")
            .Split('\r', StringSplitOptions.RemoveEmptyEntries);

        // ── MSH ──────────────────────────────────────────────────────────────
        var msh = segments.FirstOrDefault(s => s.StartsWith("MSH", StringComparison.Ordinal));
        if (msh == null) return null;

        var mshF = msh.Split('|');
        // MSH field positions (1-indexed in HL7 spec) map to array index N-1
        // because the segment name "MSH" takes [0] and the field separator |
        // is MSH-1 (not stored explicitly — it IS the split character).
        // MSH-2 → mshF[1], MSH-3 → mshF[2], MSH-4 → mshF[3], MSH-9 → mshF[8]
        var msgTypeField = GetAt(mshF, 8); // MSH-9: e.g. "ORU^R01"
        if (!msgTypeField.StartsWith("ORU", StringComparison.OrdinalIgnoreCase)) return null;

        var sendingFacility  = GetAt(mshF, 3);  // MSH-4
        var messageControlId = GetAt(mshF, 9);  // MSH-10

        // ── PID ──────────────────────────────────────────────────────────────
        var pid  = segments.FirstOrDefault(s => s.StartsWith("PID", StringComparison.Ordinal));
        var pidF = pid?.Split('|') ?? [];
        // PID field positions map to array index = field number (PID-1 → pidF[1])
        var pidIdList = GetAt(pidF, 3); // PID-3: e.g. "MRN-2023-00001^^^HOSP^MR"
        var patientMrn = Component(pidIdList, 0); // first component is the ID

        // ── OBR ──────────────────────────────────────────────────────────────
        var obr  = segments.FirstOrDefault(s => s.StartsWith("OBR", StringComparison.Ordinal));
        var obrF = obr?.Split('|') ?? [];

        var accessionNumber  = GetAt(obrF, 3);  // OBR-3: filler order number
        var serviceId        = GetAt(obrF, 4);  // OBR-4: e.g. "58410-2^CBC^LN"
        var orderCode        = Component(serviceId, 0);
        var orderName        = Component(serviceId, 1);
        var orderedAtRaw     = GetAt(obrF, 6);  // OBR-6: requested date/time
        var resultsDateRaw   = GetAt(obrF, 22); // OBR-22: results report date
        var orderingProvider = GetAt(obrF, 16); // OBR-16: XCN — ID^Last^First…
        var orderingDoctorId = Component(orderingProvider, 0);

        // ── OBX segments ─────────────────────────────────────────────────────
        var observations = segments
            .Where(s => s.StartsWith("OBX", StringComparison.Ordinal))
            .Select(ParseObx)
            .ToList();

        return new ParsedOruR01(
            SendingFacility:  sendingFacility,
            MessageControlId: messageControlId,
            PatientMrn:       patientMrn,
            AccessionNumber:  accessionNumber,
            OrderCode:        string.IsNullOrWhiteSpace(orderCode)  ? null : orderCode,
            OrderName:        string.IsNullOrWhiteSpace(orderName)  ? null : orderName,
            OrderedAt:        ParseHl7DateTime(orderedAtRaw),
            ResultsDateTime:  ParseHl7DateTime(resultsDateRaw),
            OrderingDoctorId: string.IsNullOrWhiteSpace(orderingDoctorId) ? null : orderingDoctorId,
            Observations:     observations
        );
    }

    private static ParsedObx ParseObx(string segment)
    {
        var f = segment.Split('|');
        _ = int.TryParse(GetAt(f, 1), out var seq);           // OBX-1
        var obsId    = GetAt(f, 3);                            // OBX-3
        var testCode = Component(obsId, 0);
        var testName = Component(obsId, 1);
        var value    = GetAt(f, 5);                            // OBX-5
        var units    = Component(GetAt(f, 6), 0);              // OBX-6
        var refRange = GetAt(f, 7);                            // OBX-7
        var abnFlag  = GetAt(f, 8);                            // OBX-8

        return new ParsedObx(
            SequenceNumber: seq,
            TestCode:       string.IsNullOrWhiteSpace(testCode) ? obsId : testCode,
            TestName:       string.IsNullOrWhiteSpace(testName) ? testCode : testName,
            Value:          string.IsNullOrWhiteSpace(value)    ? null : value,
            Units:          string.IsNullOrWhiteSpace(units)    ? null : units,
            ReferenceRange: string.IsNullOrWhiteSpace(refRange) ? null : refRange,
            AbnormalFlag:   string.IsNullOrWhiteSpace(abnFlag)  ? null : abnFlag
        );
    }

    // Array helpers
    private static string GetAt(string[] arr, int index) =>
        index >= 0 && index < arr.Length ? arr[index] : string.Empty;

    private static string Component(string field, int index)
    {
        var parts = field.Split('^');
        return index >= 0 && index < parts.Length ? parts[index] : string.Empty;
    }

    /// <summary>
    /// Parses HL7 datetime: yyyyMMddHHmmss, yyyyMMddHHmm, yyyyMMdd
    /// Returns null if blank or unparseable.
    /// </summary>
    private static DateTime? ParseHl7DateTime(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        // Strip timezone offset if present (e.g. "+0000")
        var s = raw.Length > 14 ? raw[..14] : raw;
        string[] formats = ["yyyyMMddHHmmss", "yyyyMMddHHmm", "yyyyMMdd"];
        foreach (var fmt in formats)
            if (DateTime.TryParseExact(s[..Math.Min(s.Length, fmt.Length)], fmt,
                    null, System.Globalization.DateTimeStyles.None, out var dt))
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        return null;
    }
}

public record ParsedOruR01(
    string          SendingFacility,
    string          MessageControlId,
    string          PatientMrn,
    string          AccessionNumber,
    string?         OrderCode,
    string?         OrderName,
    DateTime?       OrderedAt,
    DateTime?       ResultsDateTime,
    string?         OrderingDoctorId,
    List<ParsedObx> Observations
);

public record ParsedObx(
    int     SequenceNumber,
    string  TestCode,
    string  TestName,
    string? Value,
    string? Units,
    string? ReferenceRange,
    string? AbnormalFlag
);
