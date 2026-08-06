using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.CustomerDiscount;

// ─── Resolve (discount + funding preview) ────────────────────────────────────
public sealed class ResolveCustomerDiscountBffQuery : AizenQuery<ResolveCustomerDiscountBffResponse>
{
    public long?    CustomerPlanId                { get; init; }
    public string?  CategoryCode                  { get; init; }
    public string   CurrencyCode                  { get; init; } = "TRY";
    public decimal  ServiceBaseAmount             { get; init; }
    public bool     ProviderConsent               { get; init; }
    public long?    ParticipantPlanSubscriptionId { get; init; }
}
public sealed class ResolveCustomerDiscountBffResponse { public CustomerDiscountResolveBffResult? Result { get; init; } }

[DocumentationInfo("Resolve customer-discount BFF query handler (BE-P6)",
    "Point-in-time customer-discount + funding-split preview (GET /customer-discounts/resolve). Read-only.")]
public sealed class ResolveCustomerDiscountBffQueryHandler
    : AizenQueryHandler<ResolveCustomerDiscountBffQuery, ResolveCustomerDiscountBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ResolveCustomerDiscountBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ResolveCustomerDiscountBffResponse?> Handle(ResolveCustomerDiscountBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ResolveCustomerDiscountAsync(
            request.CustomerPlanId, request.CategoryCode, request.CurrencyCode,
            request.ServiceBaseAmount, request.ProviderConsent, request.ParticipantPlanSubscriptionId, ct) };
}

// ─── CustomerDiscountRule: List (no paging) ──────────────────────────────────
public sealed class GetCustomerDiscountRulesListBffQuery : AizenQuery<GetCustomerDiscountRulesListBffResponse>
{
    public long?   CustomerPlanId { get; init; }
    public string? CategoryCode   { get; init; }
    public string? CurrencyCode   { get; init; }
    public string? FundingMode    { get; init; }
    public bool?   IsActive       { get; init; }
}
public sealed class GetCustomerDiscountRulesListBffResponse { public CustomerDiscountRuleListBffResult Result { get; init; } = default!; }

[DocumentationInfo("Get customer-discount rules list BFF query handler (BE-P6)",
    "Returns the customer-discount rule list (no paging) with optional plan / category / currency / funding / active " +
    "filters, forwarded as plain strings to the Payment module. Read-only.")]
public sealed class GetCustomerDiscountRulesListBffQueryHandler
    : AizenQueryHandler<GetCustomerDiscountRulesListBffQuery, GetCustomerDiscountRulesListBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetCustomerDiscountRulesListBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetCustomerDiscountRulesListBffResponse?> Handle(GetCustomerDiscountRulesListBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ListCustomerDiscountRulesAsync(
            request.CustomerPlanId, request.CategoryCode, request.CurrencyCode, request.FundingMode, request.IsActive, ct) };
}

// ─── CustomerDiscountRule: Detail (by id) ────────────────────────────────────
public sealed class GetCustomerDiscountRuleDetailBffQuery : AizenQuery<GetCustomerDiscountRuleDetailBffResponse>
{
    public long Id { get; init; }
}
public sealed class GetCustomerDiscountRuleDetailBffResponse { public CustomerDiscountRuleDetailBffDto? Rule { get; init; } }

[DocumentationInfo("Get customer-discount rule detail BFF query handler (BE-P6)",
    "Fetches a single customer-discount rule by ID from the Payment module (GET /customer-discounts/rules/{id}). " +
    "A CustomerDiscountRuleNotFound surfaces through the envelope. Read-only.")]
public sealed class GetCustomerDiscountRuleDetailBffQueryHandler
    : AizenQueryHandler<GetCustomerDiscountRuleDetailBffQuery, GetCustomerDiscountRuleDetailBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetCustomerDiscountRuleDetailBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetCustomerDiscountRuleDetailBffResponse?> Handle(GetCustomerDiscountRuleDetailBffQuery request, CancellationToken ct)
        => new() { Rule = await _payment.GetCustomerDiscountRuleDetailAsync(request.Id, ct) };
}

// ─── CustomerBenefitBudgetPolicy: List (no paging) ───────────────────────────
public sealed class GetCustomerBenefitBudgetPoliciesListBffQuery : AizenQuery<GetCustomerBenefitBudgetPoliciesListBffResponse>
{
    public long?   CustomerPlanId { get; init; }
    public string? CurrencyCode   { get; init; }
    public bool?   IsActive       { get; init; }
}
public sealed class GetCustomerBenefitBudgetPoliciesListBffResponse { public CustomerBenefitBudgetPolicyListBffResult Result { get; init; } = default!; }

[DocumentationInfo("Get customer-benefit budget policies list BFF query handler (BE-P6)",
    "Returns the per-plan benefit budget policy list (no paging) with optional plan / currency / active filters, " +
    "forwarded to the Payment module. Read-only.")]
public sealed class GetCustomerBenefitBudgetPoliciesListBffQueryHandler
    : AizenQueryHandler<GetCustomerBenefitBudgetPoliciesListBffQuery, GetCustomerBenefitBudgetPoliciesListBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetCustomerBenefitBudgetPoliciesListBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetCustomerBenefitBudgetPoliciesListBffResponse?> Handle(GetCustomerBenefitBudgetPoliciesListBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ListCustomerBenefitBudgetPoliciesAsync(
            request.CustomerPlanId, request.CurrencyCode, request.IsActive, ct) };
}

// ─── CustomerBenefitBudgetPolicy: Detail (by id) ─────────────────────────────
public sealed class GetCustomerBenefitBudgetPolicyDetailBffQuery : AizenQuery<GetCustomerBenefitBudgetPolicyDetailBffResponse>
{
    public long Id { get; init; }
}
public sealed class GetCustomerBenefitBudgetPolicyDetailBffResponse { public CustomerBenefitBudgetPolicyDetailBffDto? Policy { get; init; } }

[DocumentationInfo("Get customer-benefit budget policy detail BFF query handler (BE-P6)",
    "Fetches a single benefit budget policy by ID from the Payment module (GET /benefit-budget/policies/{id}). " +
    "A CustomerBenefitBudgetNotFound surfaces through the envelope. Read-only.")]
public sealed class GetCustomerBenefitBudgetPolicyDetailBffQueryHandler
    : AizenQueryHandler<GetCustomerBenefitBudgetPolicyDetailBffQuery, GetCustomerBenefitBudgetPolicyDetailBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetCustomerBenefitBudgetPolicyDetailBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetCustomerBenefitBudgetPolicyDetailBffResponse?> Handle(GetCustomerBenefitBudgetPolicyDetailBffQuery request, CancellationToken ct)
        => new() { Policy = await _payment.GetCustomerBenefitBudgetPolicyDetailAsync(request.Id, ct) };
}
