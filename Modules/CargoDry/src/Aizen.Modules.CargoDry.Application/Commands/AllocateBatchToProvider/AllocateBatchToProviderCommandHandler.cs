using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Commands.AllocateBatchToProvider;

[DocumentationInfo("Allocate batch to provider command handler",
    "Allocates all available kits in a CargoDry batch to a provider under the specified commercial model. " +
    "For ConsignmentSellThrough: validates active consignment agreement, enforces kit cap, " +
    "updates AllocatedKitCount on the agreement. " +
    "Creates CargoDryProviderInventoryEntity and CargoDryInventoryMovementEntity. " +
    "Idempotent: if already allocated to the same provider+agreement, returns existing result. " +
    "Phase 2 — CargoDry provider inventory (July 2026).")]
public sealed class AllocateBatchToProviderCommandHandler
    : AizenCommandHandler<AllocateBatchToProviderCommand, AllocateBatchToProviderResultDto>
{
    private readonly ICargoDryBatchRepository             _batches;
    private readonly ICargoDryKitRepository               _kits;
    private readonly ICargoDryConsignmentAgreementRepository _agreements;
    private readonly ICargoDryProviderInventoryRepository  _inventories;
    private readonly ICargoDryInventoryMovementRepository  _movements;

    public AllocateBatchToProviderCommandHandler(
        ICargoDryBatchRepository              batches,
        ICargoDryKitRepository                kits,
        ICargoDryConsignmentAgreementRepository agreements,
        ICargoDryProviderInventoryRepository   inventories,
        ICargoDryInventoryMovementRepository   movements)
    {
        _batches     = batches;
        _kits        = kits;
        _agreements  = agreements;
        _inventories = inventories;
        _movements   = movements;
    }

    public override async Task<AllocateBatchToProviderResultDto> Handle(
        AllocateBatchToProviderCommand request, CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;

        // ── Load batch ─────────────────────────────────────────────────────────
        var batch = await _batches.GetByCodeAsync(request.BatchCode, ct)
                    ?? throw new InvalidOperationException(
                        $"Batch '{request.BatchCode}' not found.");

        if (batch.IsRevoked)
            throw new InvalidOperationException(
                $"Batch '{request.BatchCode}' has been revoked and cannot be allocated.");

        // ── Idempotency check ──────────────────────────────────────────────────
        if (batch.AssignedProviderProfileId.HasValue)
        {
            if (batch.AssignedProviderProfileId.Value == request.ProviderProfileId
                && batch.CommercialModel              == request.CommercialModel)
            {
                // Already allocated to the same provider with the same model — return idempotent result.
                var existingInventory = await _inventories.GetByProviderProductBatchAsync(
                    request.ProviderProfileId, batch.ProductCode, batch.BatchCode, ct);

                int remainingKit = 0;
                if (batch.ConsignmentAgreementId.HasValue)
                {
                    var agr = await _agreements.GetByIdAsync(batch.ConsignmentAgreementId.Value, ct);
                    remainingKit = agr?.RemainingKitCount ?? 0;
                }

                return new AllocateBatchToProviderResultDto
                {
                    BatchCode                  = batch.BatchCode,
                    ProviderProfileId          = request.ProviderProfileId,
                    ProductCode                = batch.ProductCode,
                    AllocatedCount             = existingInventory?.TotalAllocated ?? batch.KitCount,
                    CommercialModel            = request.CommercialModel,
                    CommercialModelName        = request.CommercialModel.ToString(),
                    SalesChannel               = request.SalesChannel,
                    SalesChannelName           = request.SalesChannel.ToString(),
                    ConsignmentAgreementId     = batch.ConsignmentAgreementId,
                    WarehouseId                = request.WarehouseId,
                    InventoryId                = existingInventory?.Id ?? 0,
                    MovementId                 = 0,
                    RemainingAgreementKitCount = remainingKit,
                    IsIdempotentResult         = true,
                };
            }

            throw new InvalidOperationException(
                $"Batch '{request.BatchCode}' is already allocated to provider " +
                $"{batch.AssignedProviderProfileId.Value}. " +
                "Cannot reallocate to a different provider.");
        }

        // ── Load available kits ────────────────────────────────────────────────
        var availableKits = await _kits.GetAvailableByBatchCodeAsync(request.BatchCode, ct);

        if (availableKits.Count == 0)
            throw new InvalidOperationException(
                $"Batch '{request.BatchCode}' has no available kits to allocate.");

        var count = availableKits.Count;

        // ── Consignment agreement validation ───────────────────────────────────
        long? resolvedAgreementId = null;
        int   remainingKitCount   = 0;

        if (request.SalesChannel == SalesChannel.ConsignmentSellThrough)
        {
            Domain.Entities.CargoDryConsignmentAgreementEntity? agreement;

            if (request.ConsignmentAgreementId.HasValue)
            {
                agreement = await _agreements.GetByIdAsync(request.ConsignmentAgreementId.Value, ct)
                            ?? throw new InvalidOperationException(
                                $"Consignment agreement {request.ConsignmentAgreementId.Value} not found.");
            }
            else
            {
                // Auto-resolve active agreement for provider+product
                agreement = await _agreements.GetActiveForProviderProductAsync(
                    request.ProviderProfileId, batch.ProductCode, nowUtc, ct)
                            ?? throw new InvalidOperationException(
                                $"No active consignment agreement found for provider {request.ProviderProfileId} " +
                                $"and product '{batch.ProductCode}'. Create and activate one first.");
            }

            if (!agreement.CanAllocate(count, nowUtc))
                throw new InvalidOperationException(
                    $"Agreement '{agreement.AgreementCode}' cannot accommodate {count} kit(s). " +
                    $"Remaining cap: {agreement.RemainingKitCount}. " +
                    $"Agreement status: {agreement.Status}.");

            agreement.IncreaseAllocatedKitCount(count);
            resolvedAgreementId = agreement.Id;
            remainingKitCount   = agreement.RemainingKitCount;

            await _agreements.SaveChangesAsync(ct);
        }

        // ── Update batch ───────────────────────────────────────────────────────
        batch.AllocateToProvider(
            request.ProviderProfileId,
            request.CommercialModel,
            resolvedAgreementId);

        await _batches.SaveChangesAsync(ct);

        // ── Update kits ────────────────────────────────────────────────────────
        foreach (var kit in availableKits)
        {
            kit.AssignToProvider(
                request.ProviderProfileId,
                request.SalesChannel,
                request.CommercialModel,
                request.WarehouseId);
        }

        await _kits.SaveChangesAsync(ct);

        // ── Create or update inventory ─────────────────────────────────────────
        var inventory = await _inventories.GetByProviderProductBatchAsync(
            request.ProviderProfileId, batch.ProductCode, batch.BatchCode, ct);

        if (inventory is null)
        {
            inventory = CargoDryProviderInventoryEntity.Create(
                providerProfileId: request.ProviderProfileId,
                productCode:       batch.ProductCode,
                batchCode:         batch.BatchCode,
                commercialModel:   request.CommercialModel,
                salesChannel:      request.SalesChannel,
                stockLocationType: StockLocationType.ProviderWarehouse,
                initialAllocated:  count,
                nowUtc:            nowUtc);

            await _inventories.AddAsync(inventory, ct);
        }
        else
        {
            inventory.IncreaseAllocation(count, nowUtc);
            await _inventories.SaveChangesAsync(ct);
        }

        // ── Create movement record ─────────────────────────────────────────────
        var movement = CargoDryInventoryMovementEntity.Create(
            providerProfileId: request.ProviderProfileId,
            productCode:       batch.ProductCode,
            movementType:      InventoryMovementType.BatchAllocated,
            quantity:          count,
            nowUtc:            nowUtc,
            batchCode:         batch.BatchCode,
            balanceAfter:      inventory.AvailableStock,
            commercialModel:   request.CommercialModel,
            salesChannel:      request.SalesChannel,
            referenceType:     resolvedAgreementId.HasValue ? "ConsignmentAgreement" : "Batch",
            referenceId:       resolvedAgreementId ?? batch.Id,
            note:              request.Note);

        await _movements.AddAsync(movement, ct);

        return new AllocateBatchToProviderResultDto
        {
            BatchCode                  = batch.BatchCode,
            ProviderProfileId          = request.ProviderProfileId,
            ProductCode                = batch.ProductCode,
            AllocatedCount             = count,
            CommercialModel            = request.CommercialModel,
            CommercialModelName        = request.CommercialModel.ToString(),
            SalesChannel               = request.SalesChannel,
            SalesChannelName           = request.SalesChannel.ToString(),
            ConsignmentAgreementId     = resolvedAgreementId,
            WarehouseId                = request.WarehouseId,
            InventoryId                = inventory.Id,
            MovementId                 = movement.Id,
            RemainingAgreementKitCount = remainingKitCount,
            IsIdempotentResult         = false,
        };
    }
}
