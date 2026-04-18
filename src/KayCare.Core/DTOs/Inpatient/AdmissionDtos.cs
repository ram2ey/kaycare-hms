namespace KayCare.Core.DTOs.Inpatient;

public class AdmissionSummaryResponse
{
    public Guid      AdmissionId           { get; set; }
    public string    AdmissionNumber       { get; set; } = string.Empty;
    public Guid      PatientId             { get; set; }
    public string    PatientName           { get; set; } = string.Empty;
    public string    PatientMrn            { get; set; } = string.Empty;
    public string    WardName              { get; set; } = string.Empty;
    public string    BedNumber             { get; set; } = string.Empty;
    public string    AdmittingDoctorName   { get; set; } = string.Empty;
    public DateTime  AdmissionDate         { get; set; }
    public DateTime? ExpectedDischargeDate { get; set; }
    public string    Status                { get; set; } = string.Empty;
    public string    AdmissionReason       { get; set; } = string.Empty;
}

public class AdmissionDetailResponse : AdmissionSummaryResponse
{
    public Guid      BedId                   { get; set; }
    public Guid      WardId                  { get; set; }
    public string    WardType                { get; set; } = string.Empty;
    public string?   DiagnosisOnAdmission    { get; set; }
    public DateTime? ActualDischargeDate     { get; set; }
    public string?   DischargeNotes          { get; set; }
    public string?   DischargeType           { get; set; }
    public string    CreatedByName           { get; set; } = string.Empty;
    public DateTime  CreatedAt               { get; set; }
    public DateTime  UpdatedAt               { get; set; }
    public List<AdmissionTransferResponse> Transfers { get; set; } = [];
}

public class AdmissionTransferResponse
{
    public Guid     AdmissionTransferId { get; set; }
    public string   FromBedNumber       { get; set; } = string.Empty;
    public string   FromWardName        { get; set; } = string.Empty;
    public string   ToBedNumber         { get; set; } = string.Empty;
    public string   ToWardName          { get; set; } = string.Empty;
    public DateTime TransferredAt       { get; set; }
    public string?  Reason              { get; set; }
    public string   TransferredByName   { get; set; } = string.Empty;
}

public class AdmitPatientRequest
{
    public Guid    PatientId             { get; set; }
    public Guid    BedId                 { get; set; }
    public Guid    AdmittingDoctorUserId { get; set; }
    public string  AdmissionReason       { get; set; } = string.Empty;
    public string? DiagnosisOnAdmission  { get; set; }
    public DateTime? ExpectedDischargeDate { get; set; }
}

public class DischargePatientRequest
{
    public string  DischargeType  { get; set; } = "Regular";
    public string? DischargeNotes { get; set; }
}

public class TransferPatientRequest
{
    public Guid    NewBedId { get; set; }
    public string? Reason   { get; set; }
}
