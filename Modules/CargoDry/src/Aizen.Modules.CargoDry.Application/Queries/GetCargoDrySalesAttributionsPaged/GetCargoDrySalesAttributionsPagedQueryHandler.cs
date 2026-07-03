using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySalesAttributionsPaged;

[DocumentationInfo("Get CargoDry sales attributions paged query handler",
    "Returns a paged list of CargoDry kit sales attribution records. " +
    "Filterable by provider, product, batch, sales channel, commercial model, status, " +
    "settlement, and date range. " +
    "Phase 3 (July 2026): Sales Attribution & Sell-Through Settlement Foundation.")]
public sealed class GetCargoDrySalesAttributionsPagedQueryHandler
    : AizenQueryHandler<GetCargoDrySalesAttributionsPagedQuery, GetCargoDrySalesAttributionsPagedResponse>
{
    private readonly ICargoDrySalesAttributionRepository _attributions;

    public GetCargoDrySalesAttributionsPagedQueryHandler(
        ICargoDrySalesAttributionRepository attributions)
        => _attributions = attributions;

    public override async Task<GetCargoDrySalesAttributionsPagedResponse> Handle(
        GetCargoDrySalesAttributionsPagedQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;

        var (items, total) = await _attributions.GetPagedAsync(
            request.ProviderProfileId,
            request.ProductCode,
            request.BatchCode,
            request.SalesChannel,
            request.CommercialModel,
            request.Status,
            request.SellThroughSettlementId,
            request.DateFrom,
            request.DateTo,
            request.Search,
            skip,
            request.PageSize,
            ct);

        var dtos = items.Select(x => new CargoDrySalesAttributionListItemDto
        {
            Id                      = x.Id,
            KitId                   = x.KitId,
            SerialNumber            = x.SerialNumber,
            KitCode                 = x.KitCode,
            ProductCode             = x.ProductCode,
            BatchCode               = x.BatchCode,
            ProviderProfileId       = x.ProviderProfileId,
            SalesChannel            = x.SalesChannel,
            SalesChannelName        = x.SalesChannel.ToString(),
            CommercialModel         = x.CommercialModel,
            CommercialModelName     = x.CommercialModel.ToString(),
            Status                  = x.Status,
            StatusName              = x.Status.ToString(),
            SalePrice               = x.SalePrice,
            CommissionAmount        = x.CommissionAmount,
            CurrencyCode            = x.CurrencyCode,
            ProviderShareAmount     = x.ProviderShareAmount,
            IsFinanciallyResolved   = x.IsFinanciallyResolved,
            SellThroughSettlementId = x.SellThroughSettlementId,
            CreatedAtUtc            = x.CreatedAtUtc,
        }).ToList();

        return new GetCargoDrySalesAttributionsPagedResponse
        {
            PagedResult = new CargoDrySalesAttributionPagedResultDto
            {
                Items    = dtos,
                Total    = total,
                Page     = request.Page,
                PageSize = request.PageSize,
            }
        };
    }
}
