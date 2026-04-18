namespace KayCare.Core.Interfaces;

public interface IChargeCaptureService
{
    /// <summary>Captures a consultation fee charge when the doctor signs off.</summary>
    Task CaptureConsultationChargeAsync(Guid consultationId, CancellationToken ct = default);

    /// <summary>Captures one charge per test when a lab order is placed.</summary>
    Task CaptureLabOrderChargesAsync(Guid labOrderId, CancellationToken ct = default);

    /// <summary>Captures pharmacy charges for each item in a dispense event.</summary>
    Task CaptureDispenseChargesAsync(Guid prescriptionId, Guid dispenseEventId, CancellationToken ct = default);
}
