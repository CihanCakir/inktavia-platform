using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetProviderInventoryList;

[DocumentationInfo("Get provider inventory list query handler",
    "Returns paged provider inventory rows with optional filters. Phase 2 — CargoDry provider inventory.")]
public sealed class GetProviderInventoryListQueryHandler
    : AizenQueryHandler<GetProviderInventoryListQuery, CargoDryProviderInventoryPagedResultDto>
{
    private readonly ICargoDryProviderInventoryRepository _inventories;

    public GetProviderInventoryListQueryHandler(ICargoDryProviderInventoryRepository inventories)
        => _inventories = inventories;

    public override async Task<CargoDryProviderInventoryPagedResultDto> Handle(
        GetProviderInventoryListQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;

        var (items, total) = await _inventories.GetPagedAsync(
            request.ProviderProfileId,
            request.ProductCode,
            request.CommercialModel,
            request.SalesChannel,
            request.HasAvailableStock,
            request.Search,
            skip,
            request.PageSize,
            ct);

        return new CargoDryProviderInventoryPagedResultDto
        {
            Items    = items.Select(MapToListItem).ToList(),
            Total    = total,
            Page     = request.Page,
            PageSize = request.PageSize,
        };
    }

    private static CargoDryProviderInventoryListItemDto MapToListItem(
        CargoDryProviderInventoryEntity e)
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
            TotalAllocated      = e.TotalAllocated,
            TotalActivated      = e.TotalActivated,
            AvailableStock      = e.AvailableStock,
            LastMovementAtUtc   = e.LastMovementAtUtc,
            CreatedAtUtc        = e.CreatedAtUtc,
        };
}
