using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySellThroughSettlementsPaged;

[DocumentationInfo("Get CargoDry sell-through settlements paged query handler",
    "Returns a paged list of sell-through settlement records. " +
    "Filterable by provider, consignment agreement, product, status, and period range. " +
    "Phase 3 (July 2026): Sales Attribution & Sell-Through Settlement Foundation.")]
public sealed class GetCargoDrySellThroughSettlementsPagedQueryHandler
    : AizenQueryHandler<GetCargoDrySellThroughSettlementsPagedQuery, GetCargoDrySellThroughSettlementsPagedResponse>
{
    private readonly ICargoDrySellThroughSettlementRepository _settlements;

    public GetCargoDrySellThroughSettlementsPagedQueryHandler(
        ICargoDrySellThroughSettlementRepository settlements)
        => _settlements = settlements;

    public override async Task<GetCargoDrySellThroughSettlementsPagedResponse> Handle(
        GetCargoDrySellThroughSettlementsPagedQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;

        var (items, total) = await _settlements.GetPagedAsync(
            request.ProviderProfileId,
            request.ConsignmentAgreementId,
            request.ProductCode,
            request.Status,
            request.PeriodFrom,
            request.PeriodTo,
            request.Search,
            skip,
            request.PageSize,
            ct);

        var dtos = items.Select(x => new CargoDrySellThroughSettlementListItemDto
        {
            Id                     = x.Id,
            SettlementCode         = x.SettlementCode,
            ConsignmentAgreementId = x.ConsignmentAgreementId,
            ProviderProfileId      = x.ProviderProfileId,
            ProductCode            = x.ProductCode,
            BatchCode              = x.BatchCode,
            TotalKitCount          = x.TotalKitCount,
            SettledKitCount        = x.SettledKitCount,
            TotalSaleAmount        = x.TotalSaleAmount,
            ProviderPayoutAmount   = x.ProviderPayoutAmount,
            CurrencyCode           = x.CurrencyCode,
            PeriodStartUtc         = x.PeriodStartUtc,
            PeriodEndUtc           = x.PeriodEndUtc,
            Status                 = x.Status,
            StatusName             = x.Status.ToString(),
            ScheduledSettlementDate = x.ScheduledSettlementDate,
            ReadyForSettlementAtUtc = x.ReadyForSettlementAtUtc,
            CreatedAtUtc            = x.CreatedAtUtc,
        }).ToList();

        return new GetCargoDrySellThroughSettlementsPagedResponse
        {
            PagedResult = new CargoDrySellThroughSettlementPagedResultDto
            {
                Items    = dtos,
                Total    = total,
                Page     = request.Page,
                PageSize = request.PageSize,
            }
        };
    }
}
