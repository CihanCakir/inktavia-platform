using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.ProfitProtection;

/// <summary>
/// Insert-only audit of a profit-protection evaluation (§7). Persisted for ApprovedWithAdjustment / Rejected /
/// ConfigurationError (and optionally Approved). Captures the context digest + computed contributions + required
/// minimums + decision + reason. <b>On failure NO payment economics snapshot is created</b> (snapshot = P8) — this
/// log is the only record. No mutators.
/// </summary>
[DocumentationInfo("Profit protection evaluation log entity",
    "Insert-only audit of a profit-protection decision (context digest + contributions + required + decision). " +
    "Written on adjustment/rejection/config-error; no snapshot is created on failure (§7).")]
[NoMessagebusSync] // domain-authored immutable financial evaluation log — never generically writable
public sealed class ProfitProtectionEvaluationLogEntity : AizenEntityWithAudit
{
    public long?                         PolicyId         { get; private set; }
    public string                        CurrencyCode     { get; private set; } = "TRY";
    public ProfitProtectionDecisionState DecisionState    { get; private set; }
    public string?                       AdjustmentReason { get; private set; }
    public DateTime                      EvaluatedAtUtc   { get; private set; }

    // ── Context digest ──────────────────────────────────────────────────────────
    public decimal ServiceAmount                       { get; private set; }
    public decimal CustomerTotalAmount                 { get; private set; }
    public decimal ProviderNetAmount                   { get; private set; }
    public decimal RequestedPlatformFundedDiscount     { get; private set; }
    public decimal RequestedCommissionBenefitCost      { get; private set; }

    // ── Applied advantages ──────────────────────────────────────────────────────
    public decimal AppliedPlatformFundedDiscount       { get; private set; }
    public decimal AppliedCommissionBenefit            { get; private set; }
    public decimal MaximumSafePlatformFundedDiscount   { get; private set; }

    // ── Contributions + required ────────────────────────────────────────────────
    public decimal CustomerSideContributionExpected     { get; private set; }
    public decimal ProviderSideContributionExpected     { get; private set; }
    public decimal TotalTransactionContributionExpected { get; private set; }
    public decimal RequiredCustomerSideContribution     { get; private set; }
    public decimal RequiredProviderSideContribution     { get; private set; }
    public decimal RequiredTransactionContribution      { get; private set; }

    private ProfitProtectionEvaluationLogEntity() { }

    public static ProfitProtectionEvaluationLogEntity Create(
        ProfitProtectionContext ctx, ProfitProtectionEvaluation ev, DateTime evaluatedAtUtc)
        => new()
        {
            PolicyId                             = ev.PolicyId,
            CurrencyCode                         = ctx.CurrencyCode.ToUpperInvariant(),
            DecisionState                        = ev.State,
            AdjustmentReason                     = ev.AdjustmentReason,
            EvaluatedAtUtc                       = evaluatedAtUtc,

            ServiceAmount                        = ctx.ServiceAmount,
            CustomerTotalAmount                  = ctx.CustomerTotalAmount,
            ProviderNetAmount                    = ctx.ProviderNetAmount,
            RequestedPlatformFundedDiscount      = ctx.RequestedPlatformFundedCustomerDiscount,
            RequestedCommissionBenefitCost       = ctx.ProviderCommissionBenefitCost,

            AppliedPlatformFundedDiscount        = ev.AppliedPlatformFundedDiscount,
            AppliedCommissionBenefit             = ev.AppliedCommissionBenefit,
            MaximumSafePlatformFundedDiscount    = ev.MaximumSafePlatformFundedDiscount,

            CustomerSideContributionExpected     = ev.CustomerSideContributionExpected,
            ProviderSideContributionExpected     = ev.ProviderSideContributionExpected,
            TotalTransactionContributionExpected = ev.TotalTransactionContributionExpected,
            RequiredCustomerSideContribution     = ev.RequiredCustomerSideContribution,
            RequiredProviderSideContribution     = ev.RequiredProviderSideContribution,
            RequiredTransactionContribution      = ev.RequiredTransactionContribution,

            IsActive                             = true,
        };
}
