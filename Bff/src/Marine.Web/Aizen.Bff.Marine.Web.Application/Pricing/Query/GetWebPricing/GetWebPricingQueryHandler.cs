using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Web.Application.Contracts.Pricing;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Pricing.Query.GetWebPricing;

public sealed class GetWebPricingQueryHandler
    : AizenQueryHandler<GetWebPricingQuery, WebPricingDto>
{
    private const string SettlementCurrency = "TRY";

    private readonly IPaymentPlanRemoteCall _plans;
    private readonly ILogger<GetWebPricingQueryHandler> _logger;

    public GetWebPricingQueryHandler(
        IPaymentPlanRemoteCall plans, ILogger<GetWebPricingQueryHandler> logger)
    {
        _plans = plans;
        _logger = logger;
    }

    public override async Task<WebPricingDto?> Handle(
        GetWebPricingQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Active plans only (includeInactive=false default); both endpoints are anonymous on the module.
            var provider = await _plans.GetProviderPlans();
            var participant = await _plans.GetParticipantPlans();

            return new WebPricingDto
            {
                Currency = SettlementCurrency,
                ProviderPlans = (provider ?? new())
                    .OrderBy(p => p.SortOrder).ThenBy(p => p.Name)
                    .Select(WebPricingMapper.ToWebPlan)
                    .ToList(),
                ParticipantPlans = (participant ?? new())
                    .OrderBy(p => p.SortOrder).ThenBy(p => p.Name)
                    .Select(WebPricingMapper.ToWebPlan)
                    .ToList(),
            };
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Web pricing failed (status {Status}).", ex.StatusCode);
            throw new AizenBusinessException("Pricing is currently unavailable.");
        }
    }
}
