using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySalesAttributionDetail;

[DocumentationInfo("Get CargoDry sales attribution detail query handler",
    "Returns the full detail of a single sales attribution record by Id. " +
    "Phase 3 (July 2026): Sales Attribution & Sell-Through Settlement Foundation.")]
public sealed class GetCargoDrySalesAttributionDetailQueryHandler
    : AizenQueryHandler<GetCargoDrySalesAttributionDetailQuery, GetCargoDrySalesAttributionDetailResponse>
{
    private readonly ICargoDrySalesAttributionRepository _attributions;

    public GetCargoDrySalesAttributionDetailQueryHandler(
        ICargoDrySalesAttributionRepository attributions)
        => _attributions = attributions;

    public override async Task<GetCargoDrySalesAttributionDetailResponse> Handle(
        GetCargoDrySalesAttributionDetailQuery request, CancellationToken ct)
    {
        var x = await _attributions.GetByIdAsync(request.Id, ct);
        if (x is null)
            return new GetCargoDrySalesAttributionDetailResponse { Detail = null };

        return new GetCargoDrySalesAttributionDetailResponse
        {
            Detail = new CargoDrySalesAttributionDto
            {
                Id                      = x.Id,
                PublicId                = x.PublicId,
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
                ConsignmentAgreementId  = x.ConsignmentAgreementId,
                InventoryId             = x.InventoryId,
                Status                  = x.Status,
                StatusName              = x.Status.ToString(),
                SalePrice                 = x.SalePrice,
                CommissionRate            = x.CommissionRate,
                CommissionAmount          = x.CommissionAmount,
                CurrencyCode              = x.CurrencyCode,
                ProviderShareAmount       = x.ProviderShareAmount,
                PlatformShareAmount       = x.PlatformShareAmount,
                IsFinanciallyResolved     = x.IsFinanciallyResolved,
                FinancialResolvedAtUtc    = x.FinancialResolvedAtUtc,
                FinancialResolvedByUserId = x.FinancialResolvedByUserId,
                ResolutionNote            = x.ResolutionNote,
                SellThroughSettlementId   = x.SellThroughSettlementId,
                AttributedAt            = x.AttributedAt,
                AttributedByUserId      = x.AttributedByUserId,
                ReviewNote              = x.ReviewNote,
                ReviewedByUserId        = x.ReviewedByUserId,
                ReviewedAt              = x.ReviewedAt,
                CreatedAtUtc            = x.CreatedAtUtc,
            }
        };
    }
}
