using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Application.Commands.AdjustProviderInventory;

namespace Aizen.Modules.CargoDry.Application.Queries.GetProviderInventoryDetail;

[DocumentationInfo("Get provider inventory detail query handler",
    "Returns aggregate inventory summary for a single provider across all products/batches. " +
    "Phase 2 — CargoDry provider inventory.")]
public sealed class GetProviderInventoryDetailQueryHandler
    : AizenQueryHandler<GetProviderInventoryDetailQuery, CargoDryProviderInventoryDetailDto>
{
    private readonly ICargoDryProviderInventoryRepository _inventories;

    public GetProviderInventoryDetailQueryHandler(ICargoDryProviderInventoryRepository inventories)
        => _inventories = inventories;

    public override async Task<CargoDryProviderInventoryDetailDto> Handle(
        GetProviderInventoryDetailQuery request, CancellationToken ct)
    {
        var rows = await _inventories.GetByProviderAsync(request.ProviderProfileId, ct);

        return new CargoDryProviderInventoryDetailDto
        {
            ProviderProfileId  = request.ProviderProfileId,
            InventoryRows      = rows.Select(AdjustProviderInventoryCommandHandler.MapToDto).ToList(),
            TotalAllocated     = rows.Sum(r => r.TotalAllocated),
            TotalActivated     = rows.Sum(r => r.TotalActivated),
            TotalRevoked       = rows.Sum(r => r.TotalRevoked),
            TotalReturned      = rows.Sum(r => r.TotalReturned),
            TotalAdjusted      = rows.Sum(r => r.TotalAdjusted),
            TotalAvailable     = rows.Sum(r => r.AvailableStock),
            LastMovementAtUtc  = rows.Count > 0 ? rows.Max(r => r.LastMovementAtUtc) : null,
        };
    }
}
