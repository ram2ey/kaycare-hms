namespace KayCare.Core.Constants;

public static class AuditActions
{
    public const string PatientView   = "Patient.View";
    public const string PatientCreate = "Patient.Create";
    public const string PatientUpdate = "Patient.Update";

    public const string BillCreate     = "Bill.Create";
    public const string BillIssue      = "Bill.Issue";
    public const string BillPayment    = "Bill.Payment";
    public const string BillDiscount   = "Bill.Discount";
    public const string BillAdjustment = "Bill.Adjustment";
    public const string BillWriteOff   = "Bill.WriteOff";
    public const string BillCancel     = "Bill.Cancel";
    public const string BillVoid       = "Bill.Void";

    public const string CreditNoteCreate  = "CreditNote.Create";
    public const string CreditNoteApprove = "CreditNote.Approve";
    public const string CreditNoteApply   = "CreditNote.Apply";
    public const string CreditNoteVoid    = "CreditNote.Void";

    public const string RefundCreate  = "Refund.Create";
    public const string RefundProcess = "Refund.Process";
    public const string RefundCancel  = "Refund.Cancel";

    public const string ClaimCreate  = "Claim.Create";
    public const string ClaimSubmit  = "Claim.Submit";
    public const string ClaimApprove = "Claim.Approve";
    public const string ClaimReject  = "Claim.Reject";
    public const string ClaimCancel  = "Claim.Cancel";

    public const string ChargeCapture = "Charge.Capture";

    public const string StockMovementRecord         = "StockMovement.Record";
    public const string StockMovementDispenseDeduct = "StockMovement.DispenseDeduct";

    public const string PrescriptionCreate          = "Prescription.Create";
    public const string PrescriptionDispense        = "Prescription.Dispense";
    public const string PrescriptionPartialDispense = "Prescription.PartialDispense";
    public const string PrescriptionCancel          = "Prescription.Cancel";

    public const string AdmissionAdmit                   = "Admission.Admit";
    public const string AdmissionDischarge               = "Admission.Discharge";
    public const string AdmissionTransfer                = "Admission.Transfer";
    public const string AdmissionUpdateDischargeSummary  = "Admission.UpdateDischargeSummary";
}
