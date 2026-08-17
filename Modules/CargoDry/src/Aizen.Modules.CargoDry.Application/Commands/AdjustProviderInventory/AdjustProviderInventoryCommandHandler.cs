using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Commands.AdjustProviderInventory;

[DocumentationInfo("Adjust provider inventory command handler",
    "Admin manual correction for provider CargoDry stock. " +
    "Validates the adjustment does not drive AvailableStock below zero. " +
    "Creates a ManualAdjustment movement row. " +
    "Phase 2 — CargoDry provider inventory (July 2026).")]
public sealed class AdjustProviderInventoryCommandHandler
    : AizenCommandHandler<AdjustProviderInventoryCommand, CargoDryProviderInventoryDto>
{
    private readonly ICargoDryProviderInventoryRepository _inventories;
    private readonly ICargoDryInventoryMovementRepository _movements;

    public AdjustProviderInventoryCommandHandler(
        ICargoDryProviderInventoryRepository inventories,
        ICargoDryInventoryMovementRepository movements)
    {
        _inventories = inventories;
        _movements   = movements;
    }

    public override async Task<CargoDryProviderInventoryDto> Handle(
        AdjustProviderInventoryCommand request, CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;

        var inventory = await _inventories.GetByProviderProductBatchAsync(
            request.ProviderProfileId, request.ProductCode, request.BatchCode, ct)
            ?? throw new InvalidOperationException(
                $"No inventory record found for provider {request.ProviderProfileId}, " +
                $"product '{request.ProductCode}'" +
                (request.BatchCode is not null ? $", batch '{request.BatchCode}'" : "") + ".");

        // Domain method validates invariant (AvailableStock + quantity >= 0)
        inventory.Adjust(request.AdjustmentQuantity, nowUtc);

        await _inventories.SaveChangesAsync(ct);

        var movement = CargoDryInventoryMovementEntity.Create(
            providerProfileId: request.ProviderProfileId,
            productCode:       request.ProductCode,
            movementType:      InventoryMovementType.ManualAdjustment,
            quantity:          request.AdjustmentQuantity,
            nowUtc:            nowUtc,
            batchCode:         request.BatchCode,
            balanceAfter:      inventory.AvailableStock,
            note:              request.Reason);

        await _movements.AddAsync(movement, ct);

        return MapToDto(inventory);
    }

    internal static CargoDryProviderInventoryDto MapToDto(CargoDryProviderInventoryEntity e)
        => new()
        {
            Id                  = e.Id,
            ProviderProfileId   = e.ProviderProfileId,
            ProductCode         = e.ProductCode,
            BatchCode           = e.BatchCode,
            CommercialModel     = e.CommercialModel,
            CommercialModelName = e.CommercialModel.ToString(),
            SalesChannel        = e.SalesChannel,
            SalesChannelName    = e.SalesChannel.ToString(),
            StockLocationType   = e.StockLocationType,
            StockLocationName   = e.StockLocationType.ToString(),
            TotalAllocated      = e.TotalAllocated,
            TotalActivated      = e.TotalActivated,
            TotalRevoked        = e.TotalRevoked,
            TotalReturned       = e.TotalReturned,
            TotalAdjusted       = e.TotalAdjusted,
            AvailableStock      = e.AvailableStock,
            LastMovementAtUtc   = e.LastMovementAtUtc,
            CreatedAtUtc        = e.CreatedAtUtc,
            UpdatedAtUtc        = e.UpdatedAtUtc,
        };
}
