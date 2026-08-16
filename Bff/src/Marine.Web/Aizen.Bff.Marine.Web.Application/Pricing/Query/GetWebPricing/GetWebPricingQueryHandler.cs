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

    private readonly IPaymentRemoteCall _payment;
    private readonly ILogger<GetWebPricingQueryHandler> _logger;

    public GetWebPricingQueryHandler(
        IPaymentRemoteCall payment, ILogger<GetWebPricingQueryHandler> logger)
    {
        _payment = payment;
        _logger = logger;
    }

    public override async Task<WebPricingDto?> Handle(
        GetWebPricingQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Active plans only (includeInactive=false default); both endpoints are anonymous on the module.
            var provider = await _payment.GetProviderPlans();
            var participant = await _payment.GetParticipantPlans();

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
                // M1 — published commercial terms folded into the same response (best-effort: a terms failure leaves
                // Terms null and still renders the plan tiers).
                Terms = await TryGetTermsAsync(),
            };
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Web pricing failed (status {Status}).", ex.StatusCode);
            throw new AizenBusinessException("Pricing is currently unavailable.");
        }
    }

    private async Task<WebPricingTermsDto?> TryGetTermsAsync()
    {
        try
        {
            var terms = await _payment.GetPublicPricingTerms();
            return terms is null ? null : WebPricingMapper.ToWebTerms(terms);
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Web pricing terms unavailable (status {Status}); returning plans only.", ex.StatusCode);
            return null;
        }
    }
}
