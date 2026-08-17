using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.CommissionBenefit;

/// <summary>
/// A commission-benefit entitlement granted to a provider (BE-P7): tracks how much of the benefit rule's usage / eligible
/// GMV has been reserved / consumed. <b>Not a wallet</b> — a commercial-advantage control ledger (mirrors the BE-P6 budget).
/// Reserve → Consume/Release; <see cref="Version"/> is an optimistic-concurrency token so concurrent offers cannot
/// double-reserve/consume and a duplicate webhook cannot double-consume (§19.15).
/// </summary>
[DocumentationInfo("Provider commission benefit entitlement entity",
    "Granted-to-provider usage/GMV ledger for a commission-benefit rule. Reserve/Consume/Release with optimistic " +
    "concurrency (Version). Not a wallet.")]
public sealed class ProviderCommissionBenefitEntitlementEntity : AizenEntityWithAudit
{
    public string?  EntitlementCode   { get; private set; }
    public long     ProviderProfileId { get; private set; }
    public long     BenefitRuleId     { get; private set; }
    public DateTime GrantedFrom       { get; private set; }
    public DateTime? GrantedTo        { get; private set; }

    public long?    UsageLimit         { get; private set; }
    public long     UsedCount          { get; private set; }
    public long     ReservedCount      { get; private set; }
    public decimal? MaximumEligibleGMV { get; private set; }
    public decimal  ConsumedGMV        { get; private set; }
    public decimal  ReservedGMV        { get; private set; }

    public ProviderCommissionBenefitEntitlementStatus Status { get; private set; }

    /// <summary>Optimistic-concurrency token (EF <c>IsConcurrencyToken</c>); incremented on every mutation.</summary>
    public long Version { get; private set; }

    /// <summary>Remaining applications (null = unlimited usage).</summary>
    public long? RemainingUsage => UsageLimit.HasValue ? UsageLimit.Value - UsedCount - ReservedCount : null;
    /// <summary>Remaining eligible GMV (null = unlimited GMV).</summary>
    public decimal? RemainingGmv => MaximumEligibleGMV.HasValue ? MaximumEligibleGMV.Value - ConsumedGMV - ReservedGMV : null;

    private ProviderCommissionBenefitEntitlementEntity() { }

    public static ProviderCommissionBenefitEntitlementEntity Grant(
        string? entitlementCode, long providerProfileId, long benefitRuleId,
        DateTime grantedFrom, DateTime? grantedTo, long? usageLimit, decimal? maximumEligibleGmv)
        => new()
        {
            EntitlementCode    = entitlementCode,
            ProviderProfileId  = providerProfileId,
            BenefitRuleId      = benefitRuleId,
            GrantedFrom        = grantedFrom,
            GrantedTo          = grantedTo,
            UsageLimit         = usageLimit,
            UsedCount          = 0,
            ReservedCount      = 0,
            MaximumEligibleGMV = maximumEligibleGmv,
            ConsumedGMV        = 0m,
            ReservedGMV        = 0m,
            Status             = ProviderCommissionBenefitEntitlementStatus.Active,
            Version            = 0,
            IsActive           = true,
        };

    public bool IsUsable(DateTime atUtc) =>
        Status == ProviderCommissionBenefitEntitlementStatus.Active
        && GrantedFrom <= atUtc && (GrantedTo == null || GrantedTo > atUtc);

    // ── Reserve / Consume / Release (§19.5 controls 5 usage/GMV) ────────────────

    /// <summary>Holds one application + <paramref name="gmvAmount"/> of eligible GMV. Throws Exhausted if over a limit.</summary>
    public ProviderCommissionBenefitUsageEntity Reserve(decimal gmvAmount, decimal benefitAmount, string contextRef, DateTime atUtc)
    {
        if (Status != ProviderCommissionBenefitEntitlementStatus.Active)
            throw Exhausted("Entitlement is not active.");
        if (RemainingUsage is { } ru && ru < 1)
            throw Exhausted("Usage limit reached.");
        if (RemainingGmv is { } rg && rg < gmvAmount)
            throw Exhausted($"Eligible GMV remaining ({rg}) is less than requested ({gmvAmount}).");

        ReservedCount++;
        ReservedGMV += gmvAmount;
        Version++;
        return ProviderCommissionBenefitUsageEntity.CreateReserved(Id, contextRef, gmvAmount, benefitAmount, atUtc);
    }

    public void Consume(ProviderCommissionBenefitUsageEntity usage, DateTime atUtc)
    {
        GuardOwned(usage);
        usage.MarkConsumed(atUtc);   // throws if not Reserved
        ReservedCount--;
        UsedCount++;
        ReservedGMV -= usage.GmvAmount;
        ConsumedGMV += usage.GmvAmount;
        Version++;
        RefreshStatus();
    }

    public void Release(ProviderCommissionBenefitUsageEntity usage, DateTime atUtc)
    {
        GuardOwned(usage);
        usage.MarkReleased(atUtc);   // throws if not Reserved
        ReservedCount--;
        ReservedGMV -= usage.GmvAmount;
        Version++;
    }

    public void Revoke()
    {
        Status   = ProviderCommissionBenefitEntitlementStatus.Revoked;
        IsActive = false;
    }

    private void RefreshStatus()
    {
        var usageExhausted = UsageLimit.HasValue && (UsedCount + ReservedCount) >= UsageLimit.Value;
        var gmvExhausted   = MaximumEligibleGMV.HasValue && (ConsumedGMV + ReservedGMV) >= MaximumEligibleGMV.Value;
        if (usageExhausted || gmvExhausted)
            Status = ProviderCommissionBenefitEntitlementStatus.Exhausted;
    }

    private void GuardOwned(ProviderCommissionBenefitUsageEntity usage)
    {
        if (usage.EntitlementId != Id)
            throw new AizenBusinessException((int)PaymentErrorCode.ProviderCommissionBenefitUsageNotFound,
                "Usage row does not belong to this entitlement.");
    }

    private static AizenBusinessException Exhausted(string m) =>
        new((int)PaymentErrorCode.ProviderCommissionBenefitExhausted, m);
}
