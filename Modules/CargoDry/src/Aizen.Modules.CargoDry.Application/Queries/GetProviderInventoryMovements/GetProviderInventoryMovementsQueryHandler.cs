using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetProviderInventoryMovements;

[DocumentationInfo("Get provider inventory movements query handler",
    "Returns paged inventory movement ledger with optional provider/product/batch/type/date filters. " +
    "Phase 2 — CargoDry provider inventory.")]
public sealed class GetProviderInventoryMovementsQueryHandler
    : AizenQueryHandler<GetProviderInventoryMovementsQuery, CargoDryInventoryMovementPagedResultDto>
{
    private readonly ICargoDryInventoryMovementRepository _movements;

    public GetProviderInventoryMovementsQueryHandler(ICargoDryInventoryMovementRepository movements)
        => _movements = movements;

    public override async Task<CargoDryInventoryMovementPagedResultDto> Handle(
        GetProviderInventoryMovementsQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;

        var (items, total) = await _movements.GetPagedAsync(
            request.ProviderProfileId,
            request.ProductCode,
            request.BatchCode,
            request.MovementType,
            request.DateFrom,
            request.DateTo,
            skip,
            request.PageSize,
            ct);

        return new CargoDryInventoryMovementPagedResultDto
        {
            Items    = items.Select(MapToDto).ToList(),
            Total    = total,
            Page     = request.Page,
            PageSize = request.PageSize,
        };
    }

    private static CargoDryInventoryMovementDto MapToDto(CargoDryInventoryMovementEntity e)
        => new()
        {
            Id                  = e.Id,
            ProviderProfileId   = e.ProviderProfileId,
            ProductCode         = e.ProductCode,
            BatchCode           = e.BatchCode,
            KitId               = e.KitId,
            MovementType        = e.MovementType,
            MovementTypeName    = e.MovementType.ToString(),
            Quantity            = e.Quantity,
            BalanceAfter        = e.BalanceAfter,
            CommercialModel     = e.CommercialModel,
            CommercialModelName = e.CommercialModel?.ToString(),
            SalesChannel        = e.SalesChannel,
            SalesChannelName    = e.SalesChannel?.ToString(),
            ReferenceType       = e.ReferenceType,
            ReferenceId         = e.ReferenceId,
            Note                = e.Note,
            CreatedAtUtc        = e.CreatedAtUtc,
            CreatedByUserId     = e.CreatedByUserId,
        };
}
