using KayCare.Core.DTOs.Pharmacy;

namespace KayCare.Core.Interfaces;

public interface IStockMovementService
{
    Task<StockMovementResponse> RecordMovementAsync(Guid drugInventoryId, string movementType, int quantity, Guid? referenceId = null, string? referenceType = null, string? notes = null, CancellationToken ct = default);
    Task<List<StockMovementResponse>> GetMovementsForDrugAsync(Guid drugInventoryId, CancellationToken ct = default);

    /// <summary>
    /// Called automatically after a prescription dispense. Matches each item to inventory —
    /// preferring the linked DrugInventoryId when present, falling back to a case-insensitive
    /// name match otherwise — and deducts stock for each match found. Items with no match never
    /// block the dispense, but are logged and audited rather than silently skipped (F1.3/F6.1).
    /// </summary>
    Task DeductForDispenseAsync(Guid prescriptionId, IEnumerable<(Guid? DrugInventoryId, string MedicationName, int Quantity)> dispensedItems, CancellationToken ct = default);
}
