using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.RefundAllocation;

/// <summary>
/// BE-P10 §7.1 — the versioned, single-active-per-currency, admin-configurable refund-allocation policy. Per
/// <see cref="RefundCause"/> child rule it fixes the platform-fee refund mode; it also carries the provider
/// <see cref="NegativeBalanceLimit"/> (§7.4). No baked constants — the MVP §7.2 table is a seed.
/// </summary>
[DocumentationInfo("Refund allocation policy entity",
    "Versioned single-active refund-allocation policy: per-cause platform-fee refund mode + provider negative-balance limit.")]
public sealed class RefundAllocationPolicyEntity : AizenEntityWithAudit
{
    public string               CurrencyCode        { get; private set; } = "TRY";
    public DateTime             EffectiveFrom       { get; private set; }
    public DateTime?            EffectiveTo         { get; private set; }
    public CommissionRuleStatus Status              { get; private set; }
    public string?              PolicyCode          { get; private set; }
    public string?              PolicyName          { get; private set; }
    public string?              Notes               { get; private set; }
    /// <summary>§7.4 — a provider's negative balance may not exceed this (blocks payout + acceptance). 0 = no limit.</summary>
    public decimal              NegativeBalanceLimit { get; private set; }

    private readonly List<RefundAllocationPolicyRuleEntity> _rules = new();
    public IReadOnlyCollection<RefundAllocationPolicyRuleEntity> Rules => _rules.AsReadOnly();

    private RefundAllocationPolicyEntity() { }

    public static RefundAllocationPolicyEntity Create(
        string currencyCode, decimal negativeBalanceLimit,
        DateTime effectiveFrom, DateTime? effectiveTo,
        string? policyCode, string? policyName = null, string? notes = null)
    {
        if (negativeBalanceLimit < 0m)
            throw new AizenBusinessException((int)PaymentErrorCode.RefundAllocationPolicyInvalid, "NegativeBalanceLimit cannot be negative.");
        if (effectiveTo.HasValue && effectiveTo.Value <= effectiveFrom)
            throw new AizenBusinessException((int)PaymentErrorCode.RefundAllocationPolicyInvalid, "effectiveTo must be after effectiveFrom.");

        return new RefundAllocationPolicyEntity
        {
            CurrencyCode         = currencyCode.ToUpperInvariant(),
            NegativeBalanceLimit = negativeBalanceLimit,
            EffectiveFrom        = effectiveFrom,
            EffectiveTo          = effectiveTo,
            PolicyCode           = policyCode,
            PolicyName           = policyName,
            Notes                = notes,
            IsActive             = true,
            Status               = DeriveStatus(effectiveFrom, effectiveTo),
        };
    }

    public RefundAllocationPolicyEntity AddRule(RefundCause cause, PlatformFeeRefundMode mode, decimal? fixedPlatformFeeAmount = null)
    {
        if (_rules.Any(r => r.Cause == cause))
            throw new AizenBusinessException((int)PaymentErrorCode.RefundAllocationPolicyInvalid, $"Duplicate rule for cause {cause}.");
        _rules.Add(RefundAllocationPolicyRuleEntity.Create(cause, mode, fixedPlatformFeeAmount));
        return this;
    }

    /// <summary>Resolves the platform-fee refund mode for a cause (defaults to <c>Full</c> when no explicit rule).</summary>
    public (PlatformFeeRefundMode Mode, decimal? FixedAmount) RuleFor(RefundCause cause)
    {
        var r = _rules.FirstOrDefault(x => x.Cause == cause);
        return r is null ? (PlatformFeeRefundMode.Full, null) : (r.PlatformFeeRefundMode, r.FixedPlatformFeeAmount);
    }

    public bool IsEffective(DateTime atUtc) =>
        IsActive && EffectiveFrom <= atUtc && (EffectiveTo == null || EffectiveTo > atUtc);

    public void Deactivate() { IsActive = false; Status = CommissionRuleStatus.Inactive; }
    public void SetPolicyCode(string policyCode) => PolicyCode = policyCode;

    /// <summary>BE-P11/P10 admin edit — the tunable header fields (negative-balance limit, effective window, notes). Per-cause
    /// rules are set at create time (rule editing is a follow-up). Throws <see cref="PaymentErrorCode.RefundAllocationPolicyInvalid"/> on bad input.</summary>
    public void UpdateHeader(decimal negativeBalanceLimit, DateTime effectiveFrom, DateTime? effectiveTo, string? notes)
    {
        if (negativeBalanceLimit < 0m)
            throw new AizenBusinessException((int)PaymentErrorCode.RefundAllocationPolicyInvalid, "NegativeBalanceLimit must be ≥ 0.");
        if (effectiveTo.HasValue && effectiveTo.Value <= effectiveFrom)
            throw new AizenBusinessException((int)PaymentErrorCode.RefundAllocationPolicyInvalid, "EffectiveTo must be after EffectiveFrom.");
        NegativeBalanceLimit = negativeBalanceLimit;
        EffectiveFrom = effectiveFrom;
        EffectiveTo   = effectiveTo;
        Notes         = notes;
        Status        = DeriveStatus(effectiveFrom, effectiveTo);
    }

    private static CommissionRuleStatus DeriveStatus(DateTime effectiveFrom, DateTime? effectiveTo)
    {
        var now = DateTime.UtcNow;
        if (effectiveFrom > now) return CommissionRuleStatus.Scheduled;
        if (effectiveTo.HasValue && effectiveTo.Value <= now) return CommissionRuleStatus.Expired;
        return CommissionRuleStatus.Active;
    }
}

/// <summary>BE-P10 — one per-cause rule under a <see cref="RefundAllocationPolicyEntity"/>.</summary>
public sealed class RefundAllocationPolicyRuleEntity : AizenEntityWithAudit
{
    public long                  RefundAllocationPolicyId { get; private set; }
    public RefundCause           Cause                    { get; private set; }
    public PlatformFeeRefundMode PlatformFeeRefundMode    { get; private set; }
    public decimal?              FixedPlatformFeeAmount   { get; private set; }

    private RefundAllocationPolicyRuleEntity() { }

    internal static RefundAllocationPolicyRuleEntity Create(RefundCause cause, PlatformFeeRefundMode mode, decimal? fixedAmount)
        => new() { Cause = cause, PlatformFeeRefundMode = mode, FixedPlatformFeeAmount = fixedAmount, IsActive = true };
}

/// <summary>BE-P10 §7.1 — pure single-active resolver (mirrors ProfitProtectionPolicyResolver).</summary>
public static class RefundAllocationPolicyResolver
{
    public static RefundAllocationPolicyEntity? Resolve(
        IEnumerable<RefundAllocationPolicyEntity> activePolicies, string currency, DateTime atUtc)
    {
        var matches = activePolicies
            .Where(p => string.Equals(p.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase) && p.IsEffective(atUtc))
            .ToList();

        if (matches.Count > 1)
            throw new AizenBusinessException((int)PaymentErrorCode.RefundAllocationPolicyConflict,
                $"Overlapping refund-allocation policies for {currency} at {atUtc:o}: {matches.Count} active. Exactly one must apply.");

        return matches.Count == 1 ? matches[0] : null;
    }

    public static RefundAllocationPolicyEntity? FindOverlappingConflict(
        RefundAllocationPolicyEntity candidate, IEnumerable<RefundAllocationPolicyEntity> existingActive)
        => existingActive.FirstOrDefault(e =>
            e.Id != candidate.Id && e.IsActive
            && string.Equals(e.CurrencyCode, candidate.CurrencyCode, StringComparison.OrdinalIgnoreCase)
            && candidate.EffectiveFrom < (e.EffectiveTo ?? DateTime.MaxValue)
            && e.EffectiveFrom < (candidate.EffectiveTo ?? DateTime.MaxValue));
}

/// <summary>BE-P10 §7.1 — reconciles the operational <see cref="RefundReason"/> with the economic <see cref="RefundCause"/>.</summary>
public static class RefundCauseMap
{
    public static RefundCause FromReason(RefundReason reason) => reason switch
    {
        RefundReason.ProviderFailedToDeliver or RefundReason.OrganizerCancel => RefundCause.ProviderCancelled,
        RefundReason.ServiceRequestCancelled or RefundReason.UserCancel or RefundReason.MutualAgreement => RefundCause.CustomerCancelledBeforeWork,
        RefundReason.PartialServiceDelivered => RefundCause.CustomerCancelledAfterWorkStarted,
        RefundReason.SystemError or RefundReason.ServiceNotDelivered => RefundCause.TechnicalFailure,
        RefundReason.DuplicateCharge => RefundCause.DuplicatePayment,
        RefundReason.DisputeResolvedForPayer or RefundReason.FraudConfirmed => RefundCause.DisputeCustomerFavoured,
        _ => RefundCause.AdministrativeCorrection,   // AdminForced, PriceAdjustment, CompensationCredit
    };
}
