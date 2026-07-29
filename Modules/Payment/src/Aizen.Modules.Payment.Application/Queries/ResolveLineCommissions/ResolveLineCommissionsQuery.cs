using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using Aizen.Modules.Payment.Domain.Entities.Commission;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.ResolveLineCommissions;

/// <summary>BE-S7 read-only query: resolve per-line commissions for an offer's priced lines.</summary>
public sealed class ResolveLineCommissionsQuery : AizenQuery<ResolveLineCommissionsRemoteCallResponse>
{
    public required ResolveLineCommissionsRemoteCallRequest Request { get; init; }
}

[DocumentationInfo("ResolveLineCommissionsQueryHandler",
    "Loads the active commission rule set once and delegates to the pure LineCommissionResolver (extends BE-P2 per " +
    "line). Read-only — no writes, no MarkApplied. Propagates CommissionRuleConflict.")]
public sealed class ResolveLineCommissionsQueryHandler
    : AizenQueryHandler<ResolveLineCommissionsQuery, ResolveLineCommissionsRemoteCallResponse>
{
    private readonly ICommissionRuleRepository _rules;
    public ResolveLineCommissionsQueryHandler(ICommissionRuleRepository rules) => _rules = rules;

    public override async Task<ResolveLineCommissionsRemoteCallResponse?> Handle(
        ResolveLineCommissionsQuery request, CancellationToken ct)
    {
        var req = request.Request;
        var now = DateTime.UtcNow;

        var activeRules = await _rules.GetActiveAtAsync(now, ct);

        var providerContext = new LineCommissionProviderContext(
            req.ProviderProfileId, req.ProviderPlanId, req.CurrencyCode);

        var lines = req.Lines.Select(l => new LineCommissionInput(
            LineRef:               l.LineRef,
            LineType:              l.LineType,
            ProductCode:           l.ProductCode,
            CommissionEligibility: l.CommissionEligibility,
            CategoryCode:          l.CategoryCode,
            CommissionBaseAmount:  l.CommissionBaseAmount,
            LineProviderRevenue:   l.LineProviderRevenue)).ToList();

        // Pure resolution (may throw CommissionRuleConflict — propagate).
        var result = LineCommissionResolver.Resolve(activeRules, providerContext, lines);

        return new ResolveLineCommissionsRemoteCallResponse
        {
            Lines = result.Lines.Select(r => new ResolveLineCommissionResultDto
            {
                LineRef              = r.LineRef,
                Commissionable       = r.Commissionable,
                CommissionBaseAmount = r.CommissionBaseAmount,
                ResolvedRate         = r.ResolvedRate,
                RuleCode             = r.RuleCode,
                CommissionAmount     = r.CommissionAmount,
                ProviderNet          = r.ProviderNet,
            }).ToList(),
            TransactionCommission     = result.TransactionCommission,
            TransactionProviderNet    = result.TransactionProviderNet,
            TransactionCommissionBase = result.TransactionCommissionBase,
            CurrencyCode              = result.CurrencyCode,
        };
    }
}
