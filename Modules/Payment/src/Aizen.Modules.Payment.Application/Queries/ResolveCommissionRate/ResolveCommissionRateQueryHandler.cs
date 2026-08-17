using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Commission;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.ResolveCommissionRate;

[DocumentationInfo("ResolveCommissionRateQueryHandler",
    "Resolves the effective commission rate via the BE-P2 specificity engine and returns the MATCHED rule's " +
    "rate, id, code, specificity rank, priority, and source (RuleType) — no longer request-inferred. " +
    "Throws CommissionRuleConflict on a fail-loud tie and CommissionRuleNotFound when nothing matches.")]
public sealed class ResolveCommissionRateQueryHandler
    : AizenQueryHandler<ResolveCommissionRateQuery, CommissionRateResult>
{
    private readonly ICommissionRuleRepository _rules;

    public ResolveCommissionRateQueryHandler(ICommissionRuleRepository rules)
        => _rules = rules;

    public override async Task<CommissionRateResult?> Handle(
        ResolveCommissionRateQuery request, CancellationToken ct)
    {
        var ctx = new CommissionResolveContext(
            ProviderProfileId:     request.ProviderProfileId,
            ProviderPlanId:        request.ProviderPlanId,
            CategoryCode:          request.CategoryCode,
            ProductCode:           request.ProductCode,
            LineType:              request.LineType,
            ContextType:           request.ContextType,
            CommercialModel:       request.CommercialModel,
            SalesChannel:          request.SalesChannel,
            CurrencyCode:          request.CurrencyCode,
            CommissionEligibility: request.CommissionEligibility);

        // Pure resolution — may throw CommissionRuleConflict (fail-loud tie).
        var resolution = await _rules.ResolveAsync(ctx, DateTime.UtcNow, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.CommissionRuleNotFound);

        return new CommissionRateResult(
            Rate:            resolution.Rate,
            ResolvedFrom:    resolution.Source,          // matched rule's RuleType, not request-inferred
            RuleId:          resolution.RuleId,
            RuleCode:        string.IsNullOrEmpty(resolution.RuleCode) ? null : resolution.RuleCode,
            SpecificityRank: resolution.SpecificityRank,
            Priority:        resolution.Priority);
    }
}
