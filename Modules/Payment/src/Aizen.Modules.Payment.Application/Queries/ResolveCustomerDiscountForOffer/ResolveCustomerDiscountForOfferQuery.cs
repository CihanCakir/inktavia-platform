using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using Aizen.Modules.Payment.Domain.Entities.CustomerDiscount;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.ResolveCustomerDiscountForOffer;

/// <summary>
/// BE-S6 — the SR-facing remote-call query: resolves the P6 CustomerDiscountRule (specificity + fail-loud conflict) and
/// computes the requested discount on the supplied eligible base. Pure read (mirrors S7 ResolveLineCommissions): no
/// persistence, no budget reserve/consume, no snapshot. Distinct from the P6-internal <c>ResolveCustomerDiscountQuery</c>
/// (which assembles the P5-engine benefit inputs).
/// </summary>
public sealed class ResolveCustomerDiscountForOfferQuery : AizenQuery<ResolveCustomerDiscountRemoteCallResponse>
{
    public required ResolveCustomerDiscountRemoteCallRequest Request { get; init; }
}

public sealed class ResolveCustomerDiscountForOfferQueryHandler
    : AizenQueryHandler<ResolveCustomerDiscountForOfferQuery, ResolveCustomerDiscountRemoteCallResponse>
{
    private readonly ICustomerDiscountRuleRepository _rules;
    public ResolveCustomerDiscountForOfferQueryHandler(ICustomerDiscountRuleRepository rules) => _rules = rules;

    public override async Task<ResolveCustomerDiscountRemoteCallResponse?> Handle(
        ResolveCustomerDiscountForOfferQuery query, CancellationToken ct)
    {
        var req = query.Request;
        var ctx = new CustomerDiscountResolveContext(req.CustomerPlanId, req.CategoryCode, req.CurrencyCode);

        var rule = await _rules.ResolveAsync(ctx, DateTime.UtcNow, ct);   // load + pure resolve; propagates conflict
        if (rule is null)
            return new ResolveCustomerDiscountRemoteCallResponse { Found = false };

        var requested = rule.ComputeRequestedDiscount(req.EligibleServiceBaseAmount);

        return new ResolveCustomerDiscountRemoteCallResponse
        {
            Found                   = true,
            RuleCode                = rule.RuleCode,
            DiscountType            = rule.DiscountType,
            RequestedDiscountAmount = requested,
            FundingMode             = rule.FundingMode,
            PlatformFundingRate     = rule.PlatformFundingRate ?? 0m,
            ProviderFundingRate     = rule.ProviderFundingRate ?? 0m,
            RequiresProviderConsent = rule.RequiresProviderConsent,
        };
    }
}
