using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Domain.Entities.Commission;
using Aizen.Modules.Payment.Domain.Entities.CommissionBenefit;
using Aizen.Modules.Payment.Domain.Entities.CustomerDiscount;
using Aizen.Modules.Payment.Domain.Entities.Economics;
using Aizen.Modules.Payment.Domain.Entities.PlatformFee;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// Application result of the P8/P8b calc: the pure combiner output + the resolved plan + applied rule ids + the
/// reservation targets (budget / entitlement) the create-escrow handler reserves→consumes/releases.
/// </summary>
public sealed record ServiceRequestEconomicsServiceResult(
    ServiceRequestEconomicsResult Core,
    long?                         ResolvedProviderPlanId,
    IReadOnlyList<long>           AppliedRuleIds,
    long?                         BudgetId                 = null,
    decimal                       PlatformFundedDiscount   = 0m,
    long?                         EntitlementId            = null,
    decimal                       EntitlementGmv           = 0m,
    decimal                       EntitlementBenefitAmount = 0m);

/// <summary>
/// BE-P8/P8b — the acceptance-time economy orchestrator (§19.8/§19.9). Loads the independent inputs authoritatively
/// (provider plan BE-P4, commission BE-P2, platform fee BE-P3, P6 customer discount, P7 effective commission, P5 policy),
/// then hands them to the PURE <see cref="ServiceRequestEconomicsCombiner"/> (which allocates the discount pre-tax, applies
/// the P7 effective rate, gates P5 with real inputs, and — on ApprovedWithAdjustment — recomputes at safe-max). <b>Read-only
/// + compute</b>: writes nothing. Budget/entitlement reserve→consume/release happen in the create-escrow handler.
/// </summary>
public sealed class ServiceRequestPaymentEconomicsCalculationService
{
    private readonly IProviderPlanRepository           _providerPlans;
    private readonly ICommissionRuleRepository         _commissionRules;
    private readonly PlatformFeeCalculationService     _platformFee;
    private readonly IProfitProtectionPolicyRepository _policies;
    private readonly ICustomerDiscountRuleRepository   _discountRules;
    private readonly ICustomerBenefitBudgetRepository  _budgets;
    private readonly ProviderCommissionBenefitService  _benefits;
    private readonly IProviderCommissionBenefitEntitlementRepository _entitlements;
    private readonly ILogger<ServiceRequestPaymentEconomicsCalculationService> _logger;

    public ServiceRequestPaymentEconomicsCalculationService(
        IProviderPlanRepository           providerPlans,
        ICommissionRuleRepository         commissionRules,
        PlatformFeeCalculationService     platformFee,
        IProfitProtectionPolicyRepository policies,
        ICustomerDiscountRuleRepository   discountRules,
        ICustomerBenefitBudgetRepository  budgets,
        ProviderCommissionBenefitService  benefits,
        IProviderCommissionBenefitEntitlementRepository entitlements,
        ILogger<ServiceRequestPaymentEconomicsCalculationService> logger)
    {
        _providerPlans   = providerPlans;
        _commissionRules = commissionRules;
        _platformFee     = platformFee;
        _policies        = policies;
        _discountRules   = discountRules;
        _budgets         = budgets;
        _benefits        = benefits;
        _entitlements    = entitlements;
        _logger          = logger;
    }

    public async Task<ServiceRequestEconomicsServiceResult> CalculateAsync(
        CalculateServiceRequestEconomicsRemoteCallRequest request, CancellationToken ct = default)
    {
        if (request.Lines is null || request.Lines.Count == 0)
            throw new PaymentEconomicsInvariantException("P8 requires at least one offer line.");

        var now = DateTime.UtcNow;

        // ── §19.9-7a: provider ACTIVE plan (BE-P4) ──
        var subscription = await _providerPlans.GetActiveSubscriptionAsync(request.ProviderProfileId, now, ct);
        var providerPlanId = subscription?.ProviderPlanId;

        // ── §19.9-7b: S7 line commission (base) ──
        var activeRules = await _commissionRules.GetActiveAtAsync(now, ct);
        var providerContext = new LineCommissionProviderContext(request.ProviderProfileId, providerPlanId, request.CurrencyCode);
        var commissionInputs = request.Lines.Select(l => new LineCommissionInput(
            LineRef: l.LineRef, LineType: l.CommissionLineType, ProductCode: l.ProductCode,
            CommissionEligibility: l.CommissionEligibility, CategoryCode: request.CategoryCode,
            CommissionBaseAmount: l.CommissionBaseAmount, LineProviderRevenue: l.LineProviderRevenue)).ToList();
        var s7 = LineCommissionResolver.Resolve(activeRules, providerContext, commissionInputs);
        var byRef = s7.Lines.ToDictionary(x => x.LineRef);

        foreach (var r in s7.Lines)
            if (r.Commissionable && string.IsNullOrWhiteSpace(r.RuleCode))
                throw new AizenBusinessException((int)PaymentErrorCode.ServiceRequestEconomicsCommissionUnresolved);

        // ── §19.9-2..4: resolve the P6 customer discount (authoritative) ──
        var eligibleBase = request.Lines.Where(l => l.DiscountEligible).Sum(l => l.LineGrossBeforeDiscount);
        var (discountInput, budgetId) = await ResolveCustomerDiscountAsync(request, eligibleBase, now, ct);

        // ── §19.5/§19.9-8/9: P7 effective commission (benefits OFF = no-op) ──
        var (adjustmentPp, entitlementId, entGmv) = await ResolveEffectiveCommissionAsync(request, providerPlanId, now, ct);

        // ── §19.9-11: platform fee rule (BE-P3) — the combiner recomputes the AMOUNT on the post-discount base ──
        var feeCtx = new PlatformFeeResolveContext(request.CurrencyCode, request.CategoryCode, CustomerType: null);
        var preFeeBase = request.Lines.Sum(l => l.LineGrossBeforeDiscount + l.LineVat);
        var fee = await _platformFee.CalculateAsync(preFeeBase, feeCtx, ct);

        // ── §19.9-11: profit-protection policy (BE-P5) ──
        var policy = await _policies.ResolveAsync(request.CurrencyCode, now, ct);

        // ── Build combiner lines (with discount eligibility + P7 effective rate) ──
        var combinerLines = request.Lines.Select(l =>
        {
            var r = byRef[l.LineRef];
            decimal? effRate = adjustmentPp != 0m && r.Commissionable
                ? Math.Max(r.ResolvedRate + adjustmentPp, 0m)
                : (decimal?)null;
            // S2d — carry the line's pricing attributes through untouched (resolved SR-side; descriptive only).
            var attributes = l.Attributes is { Count: > 0 }
                ? l.Attributes.Select(a => new LineAttributeSnapshotInput(
                    a.DefinitionCode, a.DataType, a.ValueLookupItemCode, a.ValueLookupItemLabel,
                    a.ValueNumber, a.ValueText, a.ValueBool, a.SortOrder)).ToList()
                : null;
            return new ServiceRequestEconomicsLine(
                LineRef: l.LineRef, ItemType: l.ItemType, PricingMethod: l.PricingMethod,
                CommissionEligibility: l.CommissionEligibility,
                LineGrossBeforeDiscount: l.LineGrossBeforeDiscount, LineVat: l.LineVat,
                Commissionable: r.Commissionable, CommissionBase: r.CommissionBaseAmount, ResolvedRate: r.ResolvedRate,
                CommissionAmount: r.CommissionAmount, ProviderNet: r.ProviderNet, RuleCode: r.RuleCode,
                DiscountEligible: l.DiscountEligible, EffectiveCommissionRate: effRate, Attributes: attributes);
        }).ToList();

        // ── The ONE combiner (allocates discount pre-tax, P7 rate, P5 gate + safe-max recompute, S8 snapshot) ──
        var core = ServiceRequestEconomicsCombiner.Combine(
            serviceRequestId: request.ServiceRequestId, currencyCode: request.CurrencyCode,
            lines: combinerLines, platformFeeRule: fee.Resolution, platformFee: fee.Breakdown,
            policy: policy, createdAtUtc: now, customerDiscount: discountInput);

        // ── Applied RuleCodes → RuleIds (for MarkApplied) ──
        var ruleIdByCode = activeRules.Where(x => !string.IsNullOrWhiteSpace(x.RuleCode))
            .GroupBy(x => x.RuleCode!).ToDictionary(g => g.Key, g => g.First().Id);
        var appliedRuleIds = core.AppliedRuleCodes.Where(ruleIdByCode.ContainsKey)
            .Select(code => ruleIdByCode[code]).Distinct().ToList();

        if (!core.CanProceed)
            _logger.LogWarning("P8 economics not approved for SR {SrId} Offer {OfferId}: {Decision} — {Reason}",
                request.ServiceRequestId, request.OfferId, core.Decision, core.Reason);

        return new ServiceRequestEconomicsServiceResult(
            core, providerPlanId, appliedRuleIds,
            BudgetId: budgetId,
            PlatformFundedDiscount: core.TotalPlatformFundedDiscount,
            EntitlementId: entitlementId,
            EntitlementGmv: entGmv,
            EntitlementBenefitAmount: core.ProviderCommissionBenefitCost);
    }

    // Resolve the P6 rule → requested + funding + budget remaining/id.
    private async Task<(ServiceRequestCustomerDiscountInput? Input, long? BudgetId)> ResolveCustomerDiscountAsync(
        CalculateServiceRequestEconomicsRemoteCallRequest request, decimal eligibleBase, DateTime now, CancellationToken ct)
    {
        var rule = await _discountRules.ResolveAsync(
            new CustomerDiscountResolveContext(request.CustomerPlanId, request.CategoryCode, request.CurrencyCode), now, ct);
        if (rule is null) return (null, null);

        var requested = rule.ComputeRequestedDiscount(eligibleBase);
        if (requested <= 0m) return (null, null);

        var consent = !rule.RequiresProviderConsent;   // narrow core: auto-consent only when the rule doesn't require it
        var platformRate = rule.FundingMode switch
        {
            CustomerDiscountFundingMode.PlatformFunded => 1m,
            CustomerDiscountFundingMode.Shared         => rule.PlatformFundingRate ?? 0m,
            _                                          => 0m,
        };
        var providerRate = rule.FundingMode switch
        {
            CustomerDiscountFundingMode.ProviderFunded => 1m,
            CustomerDiscountFundingMode.Shared         => rule.ProviderFundingRate ?? 0m,
            _                                          => 0m,
        };

        // Budget (per participant-plan subscription) — remaining caps platform funding. Absent → no cap.
        long? budgetId = null;
        decimal budgetRemaining = decimal.MaxValue;
        if (request.ParticipantPlanSubscriptionId is { } subId)
        {
            var budget = await _budgets.GetActiveBySubscriptionAsync(subId, now, ct);
            if (budget is not null) { budgetId = budget.Id; budgetRemaining = budget.RemainingAmount; }
        }

        return (new ServiceRequestCustomerDiscountInput(
            RequestedAmount: requested, FundingMode: rule.FundingMode,
            PlatformRate: platformRate, ProviderRate: providerRate, ProviderConsent: consent,
            DiscountRuleId: rule.Id, BudgetRemaining: budgetRemaining), budgetId);
    }

    // Resolve the P7 effective-commission adjustment (pp) + the entitlement to reserve. No-op when benefits are off.
    private async Task<(decimal AdjustmentPp, long? EntitlementId, decimal Gmv)> ResolveEffectiveCommissionAsync(
        CalculateServiceRequestEconomicsRemoteCallRequest request, long? providerPlanId, DateTime now, CancellationToken ct)
    {
        var baseAgg = await _commissionRules.ResolveAsync(
            new CommissionResolveContext(request.ProviderProfileId, providerPlanId, request.CategoryCode, request.CurrencyCode),
            now, ct);
        if (baseAgg is null) return (0m, null, 0m);

        var serviceAmount = request.Lines.Where(l => l.CommissionBaseAmount > 0m).Sum(l => l.CommissionBaseAmount);
        var benefitCtx = new ProviderCommissionBenefitContext(
            ProviderProfileId: request.ProviderProfileId, ProviderPlanId: providerPlanId,
            CategoryCode: request.CategoryCode, ServiceAmount: serviceAmount,
            EligibleGmvRemaining: null, CurrencyCode: request.CurrencyCode, PlanFloorRate: 0m);

        var eff = await _benefits.ResolveEffectiveAsync(baseAgg, benefitCtx, now, ct);
        if (eff.AppliedBenefitAmount <= 0m || eff.AppliedBenefitRuleIds.Count == 0)
            return (0m, null, 0m);   // benefits off → no-op

        var adjustmentPp = eff.EffectiveCommissionRate - eff.BaseRate;   // net pp change (≤0 = discount)
        var ent = await _entitlements.GetActiveByProviderAndRuleAsync(
            request.ProviderProfileId, eff.AppliedBenefitRuleIds[0], now, ct);
        return (adjustmentPp, ent?.Id, eff.BenefitedServiceAmount);
    }
}
