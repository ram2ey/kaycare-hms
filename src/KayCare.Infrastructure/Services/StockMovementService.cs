using KayCare.Core.Constants;
using KayCare.Core.DTOs.Pharmacy;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KayCare.Infrastructure.Services;

public class StockMovementService : IStockMovementService
{
    private readonly AppDbContext        _db;
    private readonly ITenantContext      _tenantContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService       _audit;
    private readonly ILogger<StockMovementService> _logger;

    public StockMovementService(
        AppDbContext db,
        ITenantContext tenantContext,
        ICurrentUserService currentUser,
        IAuditService audit,
        ILogger<StockMovementService> logger)
    {
        _db            = db;
        _tenantContext = tenantContext;
        _currentUser   = currentUser;
        _audit         = audit;
        _logger        = logger;
    }

    public async Task<StockMovementResponse> RecordMovementAsync(
        Guid    drugInventoryId,
        string  movementType,
        int     quantity,
        Guid?   referenceId   = null,
        string? referenceType = null,
        string? notes         = null,
        CancellationToken ct  = default)
    {
        if (quantity <= 0)
            throw new AppException("Quantity must be greater than zero.", 400);

        if (!StockMovementType.All.Contains(movementType))
            throw new AppException($"Invalid movement type '{movementType}'.", 400);

        var ownsTransaction = _db.Database.CurrentTransaction == null;
        var transaction = ownsTransaction ? await _db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            await _db.AcquireAdvisoryLockAsync(_tenantContext.TenantId, $"DrugInventory_{drugInventoryId}", ct);

            // Reload the drug inside the locked scope to get the latest updated stock level
            var drug = await _db.DrugInventory
                .FirstOrDefaultAsync(d => d.DrugInventoryId == drugInventoryId, ct)
                ?? throw new NotFoundException("DrugInventory", drugInventoryId);

            var previous = drug.CurrentStock;
            int next;

            if (StockMovementType.IsAdditive.Contains(movementType))
            {
                next = previous + quantity;
            }
            else
            {
                if (quantity > previous)
                    throw new AppException($"Cannot deduct {quantity} from {drug.Name}: only {previous} in stock.", 400);
                next = previous - quantity;
            }

            drug.CurrentStock = next;

            var movement = new StockMovement
            {
                TenantId        = _tenantContext.TenantId,
                DrugInventoryId = drugInventoryId,
                MovementType    = movementType,
                Quantity        = quantity,
                PreviousStock   = previous,
                NewStock        = next,
                ReferenceId     = referenceId,
                ReferenceType   = referenceType,
                Notes           = notes?.Trim(),
                CreatedByUserId = _currentUser.UserId,
                CreatedAt       = DateTime.UtcNow,
            };

            _db.StockMovements.Add(movement);
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(AuditActions.StockMovementRecord, nameof(DrugInventory), drugInventoryId, null,
                details: $"Type={movementType}; Qty={quantity}; {previous}->{next}", ct: ct);
            _logger.LogInformation("Stock movement recorded for drug {DrugInventoryId} ({DrugName}): {MovementType} {Quantity}, {Previous}->{New}",
                drugInventoryId, drug.Name, movementType, quantity, previous, next);

            if (ownsTransaction && transaction != null)
            {
                await transaction.CommitAsync(ct);
            }

            return ToResponse(movement, drug.Name);
        }
        catch
        {
            if (ownsTransaction && transaction != null)
            {
                await transaction.RollbackAsync(ct);
            }
            throw;
        }
        finally
        {
            if (ownsTransaction && transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task<List<StockMovementResponse>> GetMovementsForDrugAsync(Guid drugInventoryId, CancellationToken ct = default)
    {
        _ = await _db.DrugInventory.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DrugInventoryId == drugInventoryId, ct)
            ?? throw new NotFoundException("DrugInventory", drugInventoryId);

        return await _db.StockMovements
            .AsNoTracking()
            .Include(m => m.CreatedBy)
            .Where(m => m.DrugInventoryId == drugInventoryId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new StockMovementResponse
            {
                StockMovementId = m.StockMovementId,
                DrugInventoryId = m.DrugInventoryId,
                DrugName        = m.DrugInventory.Name,
                MovementType    = m.MovementType,
                Quantity        = m.Quantity,
                PreviousStock   = m.PreviousStock,
                NewStock        = m.NewStock,
                ReferenceId     = m.ReferenceId,
                ReferenceType   = m.ReferenceType,
                Notes           = m.Notes,
                CreatedByName   = m.CreatedBy.FirstName + " " + m.CreatedBy.LastName,
                CreatedAt       = m.CreatedAt,
            })
            .ToListAsync(ct);
    }

    public async Task DeductForDispenseAsync(
        Guid prescriptionId,
        IEnumerable<(Guid? DrugInventoryId, string MedicationName, int Quantity)> dispensedItems,
        CancellationToken ct = default)
    {
        var now    = DateTime.UtcNow;
        var userId = _currentUser.UserId;
        var items  = dispensedItems.ToList();

        // Match by the linked DrugInventoryId when present; fall back to a case-insensitive
        // name match for items prescribed before the link existed or with no catalog match.
        var idsToMatch   = items.Where(i => i.DrugInventoryId.HasValue).Select(i => i.DrugInventoryId!.Value).ToHashSet();
        var namesToMatch = items.Where(i => !i.DrugInventoryId.HasValue).Select(i => i.MedicationName.ToLower()).ToHashSet();

        var drugs = await _db.DrugInventory
            .Where(d => d.IsActive && (idsToMatch.Contains(d.DrugInventoryId) || namesToMatch.Contains(d.Name.ToLower())))
            .ToListAsync(ct);

        if (drugs.Count == 0)
        {
            // Nothing in inventory to deduct — do not block dispense, but this is no longer
            // silent: it's audit-worthy (F1.3/F6.1), since it usually means stock records are
            // out of sync with what's actually being dispensed.
            _logger.LogWarning(
                "Stock deduction skipped for prescription {PrescriptionId}: none of {ItemCount} dispensed item(s) matched an active inventory drug.",
                prescriptionId, items.Count);
            await _audit.LogAsync(AuditActions.StockMovementDispenseDeductSkipped, "Prescription", prescriptionId, null,
                details: $"No inventory match for any of {items.Count} dispensed item(s)", ct: ct);
            return;
        }

        var sortedDrugIds = drugs.Select(d => d.DrugInventoryId).OrderBy(id => id).ToList();

        var ownsTransaction = _db.Database.CurrentTransaction == null;
        var transaction = ownsTransaction ? await _db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            foreach (var drugId in sortedDrugIds)
            {
                await _db.AcquireAdvisoryLockAsync(_tenantContext.TenantId, $"DrugInventory_{drugId}", ct);
            }

            // Reload locked entities inside transaction to get latest stock levels
            var lockedDrugs = await _db.DrugInventory
                .Where(d => sortedDrugIds.Contains(d.DrugInventoryId))
                .ToListAsync(ct);

            var drugById = lockedDrugs.ToDictionary(d => d.DrugInventoryId);
            var drugByName = lockedDrugs
                .GroupBy(d => d.Name.ToLower())
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var (drugInventoryId, medName, qty) in items)
            {
                DrugInventory? drug = null;
                if (drugInventoryId.HasValue)
                    drugById.TryGetValue(drugInventoryId.Value, out drug);
                if (drug is null)
                    drugByName.TryGetValue(medName.ToLower(), out drug);

                if (drug is null)
                {
                    _logger.LogWarning(
                        "Stock deduction skipped for prescription {PrescriptionId}: drug '{MedicationName}' not found in inventory.",
                        prescriptionId, medName);
                    await _audit.LogAsync(AuditActions.StockMovementDispenseDeductSkipped, "Prescription", prescriptionId, null,
                        details: $"No inventory match for '{medName}'", ct: ct);
                    continue;
                }

                var deduct = Math.Min(qty, drug.CurrentStock);  // never go below 0
                if (deduct <= 0)
                    continue;

                var previous = drug.CurrentStock;
                drug.CurrentStock -= deduct;

                _db.StockMovements.Add(new StockMovement
                {
                    TenantId        = _tenantContext.TenantId,
                    DrugInventoryId = drug.DrugInventoryId,
                    MovementType    = StockMovementType.Dispense,
                    Quantity        = deduct,
                    PreviousStock   = previous,
                    NewStock        = drug.CurrentStock,
                    ReferenceId     = prescriptionId,
                    ReferenceType   = "Prescription",
                    CreatedByUserId = userId,
                    CreatedAt       = now,
                });
            }

            var deductedCount = _db.ChangeTracker.Entries<StockMovement>().Count(e => e.State == EntityState.Added);
            if (deductedCount > 0)
            {
                await _db.SaveChangesAsync(ct);

                await _audit.LogAsync(AuditActions.StockMovementDispenseDeduct, "Prescription", prescriptionId, null,
                    details: $"Deducted stock for {deductedCount} drug(s) dispensed", ct: ct);
                _logger.LogInformation("Stock deducted for {DrugCount} drug(s) dispensed under prescription {PrescriptionId}",
                    deductedCount, prescriptionId);
            }

            if (ownsTransaction && transaction != null)
            {
                await transaction.CommitAsync(ct);
            }
        }
        catch
        {
            if (ownsTransaction && transaction != null)
            {
                await transaction.RollbackAsync(ct);
            }
            throw;
        }
        finally
        {
            if (ownsTransaction && transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private static StockMovementResponse ToResponse(StockMovement m, string drugName) => new()
    {
        StockMovementId = m.StockMovementId,
        DrugInventoryId = m.DrugInventoryId,
        DrugName        = drugName,
        MovementType    = m.MovementType,
        Quantity        = m.Quantity,
        PreviousStock   = m.PreviousStock,
        NewStock        = m.NewStock,
        ReferenceId     = m.ReferenceId,
        ReferenceType   = m.ReferenceType,
        Notes           = m.Notes,
        CreatedByName   = string.Empty,
        CreatedAt       = m.CreatedAt,
    };
}
