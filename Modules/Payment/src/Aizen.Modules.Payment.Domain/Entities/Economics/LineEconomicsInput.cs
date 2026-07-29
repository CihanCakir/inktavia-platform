using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Economics;

/// <summary>
/// One priced offer line fed to <see cref="PaymentEconomicsSnapshotEntity.CreateFromLines"/> (§20.15). Carries the S1
/// line economics + the S7 resolved commission. Every money field is normalised through <c>MoneyMath.Round</c> in the
/// factory. Discounts are 0 in the narrow core (S6 populates them).
/// </summary>
public sealed record LineEconomicsInput(
    string                    LineRef,
    int                       ItemType,               // raw SR ServiceRequestOfferItemType value
    int                       PricingMethod,          // raw SR PricingMethod value
    decimal                   GrossBeforeDiscount,
    decimal                   CustomerDiscount,
    decimal                   ProviderFundedDiscount,
    decimal                   PlatformFundedDiscount,
    LineCommissionEligibility CommissionEligibility,
    decimal                   CommissionBase,
    decimal                   CommissionRate,
    decimal                   CommissionAmount,
    decimal                   ProviderNet,
    decimal                   LineVat,
    decimal                   LineTotal,
    long?                     RuleId,
    string?                   RuleCode,
    bool                      Commissionable,
    int                       SortOrder = 0);

/// <summary>Resolved platform fee bundle (from P3) passed to the line-first factory.</summary>
public sealed record PlatformFeeInput(
    long?   RuleId,
    decimal Rate,
    decimal Minimum,
    decimal Maximum,
    decimal Base,
    decimal Net,
    decimal Vat,
    decimal Gross);
