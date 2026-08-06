using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.ProviderCommissionBenefit;

// ─── Resolve (effective commission preview) ──────────────────────────────────
public sealed class ResolveEffectiveCommissionBffQuery : AizenQuery<ResolveEffectiveCommissionBffResponse>
{
    public long      ProviderProfileId    { get; init; }
    public long?     ProviderPlanId       { get; init; }
    public string?   CategoryCode         { get; init; }
    public decimal   ServiceAmount        { get; init; }
    public string    CurrencyCode         { get; init; } = "TRY";
    public decimal?  EligibleGmvRemaining { get; init; }
    public decimal   PlanFloorRate        { get; init; }
}
public sealed class ResolveEffectiveCommissionBffResponse { public EffectiveCommissionResolveBffResult? Result { get; init; } }

[DocumentationInfo("Resolve effective commission BFF query handler (BE-P7)",
    "Point-in-time effective-commission (base + benefit adjustments) preview (GET /commission-benefits/resolve). Read-only.")]
public sealed class ResolveEffectiveCommissionBffQueryHandler
    : AizenQueryHandler<ResolveEffectiveCommissionBffQuery, ResolveEffectiveCommissionBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ResolveEffectiveCommissionBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ResolveEffectiveCommissionBffResponse?> Handle(ResolveEffectiveCommissionBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ResolveEffectiveCommissionAsync(
            request.ProviderProfileId, request.ProviderPlanId, request.CategoryCode, request.ServiceAmount,
            request.CurrencyCode, request.EligibleGmvRemaining, request.PlanFloorRate, ct) };
}

// ─── Rule: List ──────────────────────────────────────────────────────────────
public sealed class GetProviderCommissionBenefitRulesListBffQuery : AizenQuery<ProviderCommissionBenefitRuleListBffResult>
{
    public long?   ProviderProfileId { get; init; }
    public long?   ProviderPlanId    { get; init; }
    public string? CategoryCode      { get; init; }
    public string? CurrencyCode      { get; init; }
    public bool?   Stackable         { get; init; }
    public bool?   IsActive          { get; init; }
}

[DocumentationInfo("List provider-commission-benefit rules BFF query handler (BE-P7)",
    "Lists benefit rules (GET /commission-benefits/rules) with optional filters. Falls back to an empty list if the module returns null.")]
public sealed class GetProviderCommissionBenefitRulesListBffQueryHandler
    : AizenQueryHandler<GetProviderCommissionBenefitRulesListBffQuery, ProviderCommissionBenefitRuleListBffResult>
{
    private readonly IPaymentRemoteCall _payment;
    public GetProviderCommissionBenefitRulesListBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ProviderCommissionBenefitRuleListBffResult?> Handle(GetProviderCommissionBenefitRulesListBffQuery request, CancellationToken ct)
        => await _payment.ListProviderCommissionBenefitRulesAsync(
               request.ProviderProfileId, request.ProviderPlanId, request.CategoryCode,
               request.CurrencyCode, request.Stackable, request.IsActive, ct)
           ?? new ProviderCommissionBenefitRuleListBffResult(new(), 0);
}

// ─── Rule: Detail ────────────────────────────────────────────────────────────
public sealed class GetProviderCommissionBenefitRuleDetailBffQuery : AizenQuery<ProviderCommissionBenefitRuleDetailBffResponse>
{
    public long Id { get; init; }
}
public sealed class ProviderCommissionBenefitRuleDetailBffResponse { public ProviderCommissionBenefitRuleDetailBffDto? Result { get; init; } }

[DocumentationInfo("Detail provider-commission-benefit rule BFF query handler (BE-P7)",
    "Returns a single benefit rule (GET /commission-benefits/rules/{id}). Read-only.")]
public sealed class GetProviderCommissionBenefitRuleDetailBffQueryHandler
    : AizenQueryHandler<GetProviderCommissionBenefitRuleDetailBffQuery, ProviderCommissionBenefitRuleDetailBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetProviderCommissionBenefitRuleDetailBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ProviderCommissionBenefitRuleDetailBffResponse?> Handle(GetProviderCommissionBenefitRuleDetailBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.GetProviderCommissionBenefitRuleDetailAsync(request.Id, ct) };
}

// ─── Entitlement: List ───────────────────────────────────────────────────────
public sealed class GetProviderCommissionBenefitEntitlementsListBffQuery : AizenQuery<ProviderCommissionBenefitEntitlementListBffResult>
{
    public long? ProviderProfileId { get; init; }
    public long? BenefitRuleId     { get; init; }
    public bool? IsActive          { get; init; }
}

[DocumentationInfo("List provider-commission-benefit entitlements BFF query handler (BE-P7)",
    "Lists granted entitlements (GET /commission-benefits/entitlements) with optional filters. Empty-list fallback on null.")]
public sealed class GetProviderCommissionBenefitEntitlementsListBffQueryHandler
    : AizenQueryHandler<GetProviderCommissionBenefitEntitlementsListBffQuery, ProviderCommissionBenefitEntitlementListBffResult>
{
    private readonly IPaymentRemoteCall _payment;
    public GetProviderCommissionBenefitEntitlementsListBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ProviderCommissionBenefitEntitlementListBffResult?> Handle(GetProviderCommissionBenefitEntitlementsListBffQuery request, CancellationToken ct)
        => await _payment.ListProviderCommissionBenefitEntitlementsAsync(
               request.ProviderProfileId, request.BenefitRuleId, request.IsActive, ct)
           ?? new ProviderCommissionBenefitEntitlementListBffResult(new(), 0);
}

// ─── Entitlement: Detail ─────────────────────────────────────────────────────
public sealed class GetProviderCommissionBenefitEntitlementDetailBffQuery : AizenQuery<ProviderCommissionBenefitEntitlementDetailBffResponse>
{
    public long Id { get; init; }
}
public sealed class ProviderCommissionBenefitEntitlementDetailBffResponse { public ProviderCommissionBenefitEntitlementDetailBffDto? Result { get; init; } }

[DocumentationInfo("Detail provider-commission-benefit entitlement BFF query handler (BE-P7)",
    "Returns a single entitlement (GET /commission-benefits/entitlements/{id}). Read-only.")]
public sealed class GetProviderCommissionBenefitEntitlementDetailBffQueryHandler
    : AizenQueryHandler<GetProviderCommissionBenefitEntitlementDetailBffQuery, ProviderCommissionBenefitEntitlementDetailBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetProviderCommissionBenefitEntitlementDetailBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ProviderCommissionBenefitEntitlementDetailBffResponse?> Handle(GetProviderCommissionBenefitEntitlementDetailBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.GetProviderCommissionBenefitEntitlementDetailAsync(request.Id, ct) };
}
