using KayCare.Core.DTOs.Pharmacy;

namespace KayCare.Core.Interfaces;

public interface IStockMovementService
{
    Task<StockMovementResponse> RecordMovementAsync(Guid drugInventoryId, string movementType, int quantity, Guid? referenceId = null, string? referenceType = null, string? notes = null, CancellationToken ct = default);
    Task<List<StockMovementResponse>> GetMovementsForDrugAsync(Guid drugInventoryId, CancellationToken ct = default);

    /// <summary>
    /// Called automatically after a prescription dispense.
    /// Matches medication names to inventory and deducts stock for each found entry.
    /// Silently skips items not found in inventory so dispense is never blocked.
    /// </summary>
    Task DeductForDispenseAsync(Guid prescriptionId, IEnumerable<(string MedicationName, int Quantity)> dispensedItems, CancellationToken ct = default);
}
