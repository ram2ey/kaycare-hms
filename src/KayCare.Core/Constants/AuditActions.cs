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

    public const string StockMovementRecord            = "StockMovement.Record";
    public const string StockMovementDispenseDeduct    = "StockMovement.DispenseDeduct";
    public const string StockMovementDispenseDeductSkipped = "StockMovement.DispenseDeductSkipped";

    public const string PrescriptionCreate          = "Prescription.Create";
    public const string PrescriptionDispense        = "Prescription.Dispense";
    public const string PrescriptionPartialDispense = "Prescription.PartialDispense";
    public const string PrescriptionCancel          = "Prescription.Cancel";

    public const string AdmissionAdmit                   = "Admission.Admit";
    public const string AdmissionDischarge               = "Admission.Discharge";
    public const string AdmissionTransfer                = "Admission.Transfer";
    public const string AdmissionUpdateDischargeSummary  = "Admission.UpdateDischargeSummary";

    public const string AppointmentCreate       = "Appointment.Create";
    public const string AppointmentUpdate       = "Appointment.Update";
    public const string AppointmentStatusChange = "Appointment.StatusChange";
    public const string AppointmentCancel       = "Appointment.Cancel";
    public const string AppointmentNoShow       = "Appointment.NoShow";

    public const string ConsultationCreate = "Consultation.Create";
    public const string ConsultationUpdate = "Consultation.Update";
    public const string ConsultationSign   = "Consultation.Sign";

    public const string LabTestCreate             = "LabTest.Create";
    public const string LabTestUpdate             = "LabTest.Update";
    public const string LabTestToggleActive       = "LabTest.ToggleActive";
    public const string LabOrderPlace             = "LabOrder.Place";
    public const string LabOrderItemReceiveSample = "LabOrderItem.ReceiveSample";
    public const string LabOrderItemEnterResult   = "LabOrderItem.EnterResult";
    public const string LabOrderItemSign          = "LabOrderItem.Sign";
    public const string LabOrderCriticalCallRecord = "LabOrder.CriticalCallRecord";

    public const string LabResultCreate = "LabResult.Create";

    public const string MedicationAdministrationRecord = "MedicationAdministration.Record";

    public const string NursingNoteCreate = "NursingNote.Create";
    public const string NursingNoteDelete = "NursingNote.Delete";

    public const string VitalSignsRecord = "VitalSigns.Record";

    public const string ImagingProcedureCreate       = "ImagingProcedure.Create";
    public const string ImagingProcedureUpdate       = "ImagingProcedure.Update";
    public const string ImagingProcedureToggleActive = "ImagingProcedure.ToggleActive";
    public const string RadiologyOrderPlace              = "RadiologyOrder.Place";
    public const string RadiologyOrderItemMarkAcquired   = "RadiologyOrderItem.MarkAcquired";
    public const string RadiologyOrderItemEnterReport    = "RadiologyOrderItem.EnterReport";
    public const string RadiologyOrderItemSign           = "RadiologyOrderItem.Sign";

    public const string ReferralCreate   = "Referral.Create";
    public const string ReferralUpdate   = "Referral.Update";
    public const string ReferralSend     = "Referral.Send";
    public const string ReferralAccept   = "Referral.Accept";
    public const string ReferralComplete = "Referral.Complete";
    public const string ReferralDecline  = "Referral.Decline";
    public const string ReferralCancel   = "Referral.Cancel";
}
