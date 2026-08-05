using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.PlatformFee;
using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;
using Aizen.Modules.Payment.Domain.Money;

namespace Aizen.Modules.Payment.Domain.Entities.Economics;

/// <summary>
/// One already-resolved offer line fed to <see cref="ServiceRequestEconomicsCombiner.Combine"/> — the SR-supplied pricing
/// (gross/vat/eligibility/type) fused with the S7 line-commission result. <see cref="DiscountEligible"/> (BE-S6) marks the
/// line as eligible for a customer discount; <see cref="EffectiveCommissionRate"/> (BE-P7) is the benefit-adjusted rate
/// (null → use <see cref="ResolvedRate"/>, i.e. benefits OFF = no-op).
/// </summary>
public sealed record ServiceRequestEconomicsLine(
    string                    LineRef,
    int                       ItemType,               // raw SR ServiceRequestOfferItemType value
    int                       PricingMethod,          // raw SR PricingMethod value
    LineCommissionEligibility CommissionEligibility,
    decimal                   LineGrossBeforeDiscount, // post-provider-discount, pre-tax provider revenue
    decimal                   LineVat,
    // ── S7 line-commission result ──
    bool                      Commissionable,
    decimal                   CommissionBase,
    decimal                   ResolvedRate,
    decimal                   CommissionAmount,
    decimal                   ProviderNet,
    string?                   RuleCode,
    // ── BE-S6 / BE-P7 (optional; defaults preserve the narrow-core path) ──
    bool                      DiscountEligible       = true,
    decimal?                  EffectiveCommissionRate = null,
    // ── S2d pricing attributes (descriptive; carried straight through to the line snapshot) ──
    IReadOnlyList<LineAttributeSnapshotInput>? Attributes = null,
    // ── S3 frozen FX metadata (descriptive; carried straight through to the line snapshot) ──
    LineFxSnapshotInput? Fx = null,
    // ── S4 structured travel derivation (descriptive; carried straight through to the aggregate travel snapshot) ──
    TravelSnapshotInput? Travel = null);

/// <summary>BE-P8b — the resolved P6 customer discount + funding to apply (authoritative). BudgetRemaining caps platform funding.</summary>
public sealed record ServiceRequestCustomerDiscountInput(
    decimal                     RequestedAmount,
    CustomerDiscountFundingMode FundingMode,
    decimal                     PlatformRate,
    decimal                     ProviderRate,
    bool                        ProviderConsent,
    long?                       DiscountRuleId,
    decimal                     BudgetRemaining);   // decimal.MaxValue when there is no per-customer budget

/// <summary>Output of the P8 combiner: the gate decision + (on success) the immutable, still-unsaved snapshot.</summary>
public sealed record ServiceRequestEconomicsResult(
    ProfitProtectionDecisionState   Decision,
    string?                         Reason,
    decimal                         OriginalServiceGrossAmount,
    decimal                         CustomerPayableServiceAmount,
    decimal                         CustomerTotalAmount,
    decimal                         ProviderNetTotal,
    decimal                         PlatformFeeNet,
    decimal                         PlatformFeeGross,
    decimal                         TransactionCommission,
    PaymentEconomicsSnapshotEntity? Snapshot,
    IReadOnlyList<string>           AppliedRuleCodes,
    decimal                         TotalCustomerDiscount        = 0m,
    decimal                         TotalPlatformFundedDiscount  = 0m,
    decimal                         TotalProviderFundedDiscount  = 0m,
    decimal                         ProviderCommissionBenefitCost = 0m,
    long?                           DiscountRuleId               = null,
    bool                            DiscountAdjusted             = false)
{
    public bool CanProceed => Decision is ProfitProtectionDecisionState.Approved
                                       or ProfitProtectionDecisionState.ApprovedWithAdjustment;
}

/// <summary>
/// BE-P8 / BE-P8b — the ONE combiner (§19.8/§19.9). PURE: no I/O, no mutation of any resolver's output. Fuses the
/// independently-resolved pieces (S7 line commission, P7 effective rate, P6/S6 customer discount, P3 platform fee, P5
/// policy) into the §19.9 binding order, gates zero-tolerance, and — on Approved/ApprovedWithAdjustment — builds the S8
/// immutable snapshot (8 equalities, now with non-zero discounts). On <b>ApprovedWithAdjustment</b> it re-runs the whole
/// economics with the safe-max platform-funded discount (§19.10/§19.11 — no silent change). Narrow core (no discount,
/// benefits off) is byte-identical to BE-P8.
/// </summary>
public static class ServiceRequestEconomicsCombiner
{
    public static ServiceRequestEconomicsResult Combine(
        long                              serviceRequestId,
        string                            currencyCode,
        IReadOnlyList<ServiceRequestEconomicsLine> lines,
        PlatformFeeResolution             platformFeeRule,
        PlatformFeeBreakdown              platformFee,
        ProfitProtectionPolicyEntity?     policy,
        DateTime                          createdAtUtc,
        ServiceRequestCustomerDiscountInput? customerDiscount = null)
    {
        if (lines is null || lines.Count == 0)
            throw new PaymentEconomicsInvariantException("P8 requires at least one offer line.");

        // Budget cap (§3): the platform-funded discount can never exceed the remaining benefit budget.
        var platformShare = customerDiscount is null ? 0m : PlatformShareRate(customerDiscount);
        var requested = customerDiscount?.RequestedAmount ?? 0m;
        if (customerDiscount is not null && platformShare > 0m && customerDiscount.BudgetRemaining < decimal.MaxValue)
        {
            var maxByBudget = MoneyMath.Round(customerDiscount.BudgetRemaining / platformShare);
            if (requested > maxByBudget) requested = maxByBudget;
        }

        // ── Pass 1: compute economics with the (budget-capped) requested discount ──
        var pass1 = ComputeLines(lines, platformFeeRule, platformFee, customerDiscount, requested);

        var ctx = BuildContext(currencyCode, pass1, customerDiscount);
        var evaluation = ProfitProtectionEngine.Evaluate(ctx, policy);
        if (!evaluation.CanProceed)
            return new ServiceRequestEconomicsResult(
                evaluation.State, evaluation.AdjustmentReason ?? evaluation.State.ToString(),
                pass1.OriginalServiceGross, pass1.CustomerPayable, pass1.CustomerTotal, pass1.ProviderNetTotal,
                pass1.FeeNet, pass1.FeeGross, pass1.TransactionCommission, Snapshot: null, AppliedRuleCodes: [],
                TotalCustomerDiscount: pass1.TotalCustomerDiscount,
                TotalPlatformFundedDiscount: pass1.TotalPlatform,
                TotalProviderFundedDiscount: pass1.TotalProvider,
                ProviderCommissionBenefitCost: pass1.BenefitCost,
                DiscountRuleId: customerDiscount?.DiscountRuleId);

        // ── §19.10/§19.11: ApprovedWithAdjustment → recompute with the safe-max platform-funded discount ──
        var final = pass1;
        var adjusted = false;
        if (evaluation.State == ProfitProtectionDecisionState.ApprovedWithAdjustment
            && customerDiscount is not null && platformShare > 0m
            && evaluation.AppliedPlatformFundedDiscount < pass1.TotalPlatform)
        {
            var adjustedRequested = MoneyMath.Round(evaluation.AppliedPlatformFundedDiscount / platformShare);
            if (adjustedRequested < requested)
            {
                final = ComputeLines(lines, platformFeeRule, platformFee, customerDiscount, adjustedRequested);
                adjusted = true;
            }
        }

        var snapshot = PaymentEconomicsSnapshotEntity.CreateFromLines(
            contextId:                    serviceRequestId,
            currencyCode:                 currencyCode,
            lines:                        final.LineInputs,
            platformFee:                  final.FeeInput(platformFeeRule),
            customerPayableServiceAmount: final.CustomerPayable,
            createdAtUtc:                 createdAtUtc);

        return new ServiceRequestEconomicsResult(
            evaluation.State, evaluation.AdjustmentReason,
            final.OriginalServiceGross, final.CustomerPayable, final.CustomerTotal, final.ProviderNetTotal,
            final.FeeNet, final.FeeGross, final.TransactionCommission, snapshot, final.AppliedRuleCodes,
            TotalCustomerDiscount: final.TotalCustomerDiscount,
            TotalPlatformFundedDiscount: final.TotalPlatform,
            TotalProviderFundedDiscount: final.TotalProvider,
            ProviderCommissionBenefitCost: final.BenefitCost,
            DiscountRuleId: customerDiscount?.DiscountRuleId,
            DiscountAdjusted: adjusted);
    }

    // ── One economics pass over the lines for a given requested discount (pure) ─────────────────────────
    private sealed record Pass(
        List<LineEconomicsInput> LineInputs, List<string> AppliedRuleCodes,
        decimal OriginalServiceGross, decimal ServiceVatTotal, decimal TransactionCommission,
        decimal ProviderNetTotal, decimal CustomerPayable, decimal CommissionBaseTotal,
        decimal TotalCustomerDiscount, decimal TotalPlatform, decimal TotalProvider, decimal BenefitCost,
        decimal FeeNet, decimal FeeVat, decimal FeeGross, decimal CustomerTotal)
    {
        public PlatformFeeInput FeeInput(PlatformFeeResolution rule) => new(
            RuleId: rule.RuleId, Rate: MoneyMath.RoundRate(rule.Rate ?? 0m),
            Minimum: MoneyMath.Round(rule.MinAmount ?? 0m), Maximum: MoneyMath.Round(rule.MaxAmount ?? 0m),
            Base: CustomerPayable, Net: FeeNet, Vat: FeeVat, Gross: FeeGross);
    }

    private static Pass ComputeLines(
        IReadOnlyList<ServiceRequestEconomicsLine> lines,
        PlatformFeeResolution platformFeeRule, PlatformFeeBreakdown platformFee,
        ServiceRequestCustomerDiscountInput? customerDiscount, decimal requested)
    {
        // Allocate the requested discount over the discount-eligible lines' pre-tax base (deterministic; §3).
        var eligibleIdx = new List<int>();
        for (int i = 0; i < lines.Count; i++)
            if (lines[i].DiscountEligible && lines[i].LineGrossBeforeDiscount > 0m) eligibleIdx.Add(i);

        var applied  = new decimal[lines.Count];
        var platform = new decimal[lines.Count];
        var provider = new decimal[lines.Count];
        if (customerDiscount is not null && requested > 0m && eligibleIdx.Count > 0)
        {
            var bases = eligibleIdx.Select(i => MoneyMath.Round(lines[i].LineGrossBeforeDiscount)).ToArray();
            AllocateDiscount(bases, requested, customerDiscount, applied, platform, provider, eligibleIdx);
        }

        var inputs = new List<LineEconomicsInput>(lines.Count);
        var codes  = new List<string>();
        decimal originalGross = 0m, vatTotal = 0m, commission = 0m, providerNet = 0m,
                payable = 0m, baseTotal = 0m, custDisc = 0m, plat = 0m, prov = 0m, benefit = 0m;
        int order = 0;

        for (int i = 0; i < lines.Count; i++)
        {
            var l     = lines[i];
            var gross = MoneyMath.Round(l.LineGrossBeforeDiscount);
            var origVat = MoneyMath.Round(l.LineVat);
            var dc    = MoneyMath.Round(applied[i]);
            var dpl   = MoneyMath.Round(platform[i]);
            var dp    = MoneyMath.Round(provider[i]);

            var reducedGross = gross - dc;
            var vatRate  = gross > 0m ? origVat / gross : 0m;
            var vat      = MoneyMath.Round(reducedGross * vatRate);
            var cbase    = l.Commissionable ? reducedGross : 0m;
            var baseRate = MoneyMath.RoundRate(l.ResolvedRate);
            var effRate  = MoneyMath.RoundRate(l.EffectiveCommissionRate ?? l.ResolvedRate);
            var comm     = l.Commissionable ? MoneyMath.Round(cbase * effRate) : 0m;
            var baseComm = l.Commissionable ? MoneyMath.Round(cbase * baseRate) : 0m;
            var pnet     = reducedGross - comm;
            var lineTot  = reducedGross + vat;

            originalGross += gross; vatTotal += vat; commission += comm; providerNet += pnet;
            payable += lineTot; baseTotal += cbase; custDisc += dc; plat += dpl; prov += dp;
            benefit += baseComm - comm;

            inputs.Add(new LineEconomicsInput(
                LineRef: l.LineRef, ItemType: l.ItemType, PricingMethod: l.PricingMethod,
                GrossBeforeDiscount: gross, CustomerDiscount: dc, ProviderFundedDiscount: dp, PlatformFundedDiscount: dpl,
                CommissionEligibility: l.CommissionEligibility, CommissionBase: cbase, CommissionRate: effRate,
                CommissionAmount: comm, ProviderNet: pnet, LineVat: vat, LineTotal: lineTot,
                RuleId: null, RuleCode: l.RuleCode, Commissionable: l.Commissionable, SortOrder: order++,
                Attributes: l.Attributes,     // S2d — descriptive, passed through untouched
                Fx: l.Fx,                     // S3  — frozen FX metadata, passed through untouched
                Travel: l.Travel));           // S4  — structured travel derivation, passed through untouched

            if (l.Commissionable && !string.IsNullOrWhiteSpace(l.RuleCode) && !codes.Contains(l.RuleCode!))
                codes.Add(l.RuleCode!);
        }

        // Platform fee (§19.9-10) recomputed on the POST-discount payable (VAT-inclusive Σ LineTotal).
        var fee = PlatformFeeCalculator.ComputeBreakdown(payable, platformFeeRule, platformFee.VatRate, platformFee.VatSource);
        var customerTotal = payable + fee.Gross;

        return new Pass(inputs, codes, originalGross, vatTotal, commission, providerNet, payable, baseTotal,
            custDisc, plat, prov, benefit, fee.Net, fee.Vat, fee.Gross, customerTotal);
    }

    private static ProfitProtectionContext BuildContext(
        string currency, Pass p, ServiceRequestCustomerDiscountInput? d)
        => new(
            CurrencyCode:                  currency,
            ServiceAmount:                 p.OriginalServiceGross,
            CustomerPayableServiceAmount:  p.CustomerPayable,
            CustomerTotalAmount:           p.CustomerTotal,
            ProviderNetAmount:             p.ProviderNetTotal,
            ProviderCommissionNetRevenue:  p.TransactionCommission,
            CustomerPlatformFeeNetRevenue: p.FeeNet,
            RequestedPlatformFundedCustomerDiscount: p.TotalPlatform,
            ProviderFundedCustomerDiscount:          p.TotalProvider,
            ProviderCommissionBenefitCost:           p.BenefitCost,
            // No per-customer budget (MaxValue) → unbudgeted platform spend, limited only by profit safe-max (§19.10),
            // NOT clamped to 0. A real budget clamps the safe-max to its remaining.
            CustomerBenefitBudgetRemaining:          d?.BudgetRemaining ?? 0m);

    private static decimal PlatformShareRate(ServiceRequestCustomerDiscountInput d) => d.FundingMode switch
    {
        CustomerDiscountFundingMode.PlatformFunded => 1m,
        CustomerDiscountFundingMode.Shared         => d.PlatformRate,
        _                                          => 0m,   // ProviderFunded / Supplier → no platform funding
    };

    // Deterministic pro-rata + funding split (mirrors S6 OfferCustomerDiscountAllocator, in Payment.Domain).
    private static void AllocateDiscount(
        decimal[] bases, decimal requested, ServiceRequestCustomerDiscountInput d,
        decimal[] applied, decimal[] platform, decimal[] provider, List<int> idx)
    {
        var totalBase = 0m;
        foreach (var b in bases) totalBase += b;
        if (totalBase <= 0m) return;
        requested = Math.Min(MoneyMath.Round(requested), totalBase);

        var raw = new decimal[bases.Length];
        decimal alloc = 0m; int largest = 0;
        for (int k = 0; k < bases.Length; k++)
        {
            raw[k] = MoneyMath.Round(requested * bases[k] / totalBase);
            alloc += raw[k];
            if (bases[k] > bases[largest]) largest = k;
        }
        raw[largest] += requested - alloc;
        if (raw[largest] < 0m) raw[largest] = 0m;

        for (int k = 0; k < bases.Length; k++)
        {
            var lineRaw = raw[k];
            decimal p = 0m, pr = 0m;
            switch (d.FundingMode)
            {
                case CustomerDiscountFundingMode.PlatformFunded: p = lineRaw; break;
                case CustomerDiscountFundingMode.ProviderFunded: pr = d.ProviderConsent ? lineRaw : 0m; break;
                case CustomerDiscountFundingMode.Shared:
                    p = MoneyMath.Round(lineRaw * d.PlatformRate);
                    pr = d.ProviderConsent ? lineRaw - p : 0m;
                    break;
                default: break;   // Supplier → 0 applied
            }
            var i = idx[k];
            platform[i] = p; provider[i] = pr; applied[i] = p + pr;
        }
    }
}
