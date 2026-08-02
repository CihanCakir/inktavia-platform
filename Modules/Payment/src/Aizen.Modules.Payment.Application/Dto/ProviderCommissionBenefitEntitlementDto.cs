using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Dto;

/// <summary>
/// Application-layer DTO for a provider commission-benefit entitlement (BE-P7): the granted-to-provider usage/GMV ledger
/// for a benefit rule. Read-only from the admin's point of view except grant/revoke — the usage counters
/// (UsedCount/ReservedCount/ConsumedGMV/ReservedGMV) are driven by the runtime reserve→consume/release flow.
/// <see cref="BenefitRuleCode"/> is enriched from the referenced rule so the list reads without a second call.
/// </summary>
public sealed record ProviderCommissionBenefitEntitlementDto(
    long                                          Id,
    string?                                       EntitlementCode,
    long                                          ProviderProfileId,
    long                                          BenefitRuleId,
    string?                                       BenefitRuleCode,
    string?                                       BenefitRuleName,
    DateTime                                       GrantedFrom,
    DateTime?                                      GrantedTo,
    long?                                          UsageLimit,
    long                                           UsedCount,
    long                                           ReservedCount,
    long?                                          RemainingUsage,
    decimal?                                       MaximumEligibleGMV,
    decimal                                        ConsumedGMV,
    decimal                                        ReservedGMV,
    decimal?                                       RemainingGmv,
    ProviderCommissionBenefitEntitlementStatus     Status,
    bool                                           IsActive,
    long?                                          CreateUserId,
    DateTime?                                      CreateDate,
    long?                                          ModifyUserId,
    DateTime?                                      ModifyDate
);

/// <summary>Provider commission-benefit entitlement list (no paging).</summary>
public sealed record ProviderCommissionBenefitEntitlementListResult(
    List<ProviderCommissionBenefitEntitlementDto> Items,
    int                                           Total
);
