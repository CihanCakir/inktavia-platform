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
    int                       SortOrder = 0,
    // ── S2d (descriptive; NOT part of the money math or the 8 equalities) ──
    IReadOnlyList<LineAttributeSnapshotInput>? Attributes = null,
    // ── S3 (frozen FX metadata; NOT part of the money math — the amounts above are already TRY) ──
    LineFxSnapshotInput? Fx = null);

/// <summary>
/// S3 — the frozen per-line FX record to snapshot at acceptance (§20.7). Carried from ServiceRequest through the
/// acceptance-economics request; resolved at offer-submit and never re-resolved. Descriptive/self-contained — the factory
/// folds it onto the line snapshot without any re-valuation; the economics already ran on the converted TRY amounts. The
/// factory verifies <c>ResolvedUnitPrice == round(SourceUnitPrice × AppliedRate)</c> (tamper → throw).
/// </summary>
public sealed record LineFxSnapshotInput(
    string   SourceCurrencyCode,
    string   SettlementCurrencyCode,
    decimal  SourceUnitPrice,
    decimal  AppliedRate,
    DateTime RateDate,
    decimal  ResolvedUnitPrice);

/// <summary>
/// S2d — one pricing attribute value to snapshot on a line at acceptance (§20.6). Carried from ServiceRequest through the
/// acceptance-economics request, resolved (label denormalized) SR-side. <see cref="DataType"/> is the raw SR
/// <c>PricingAttributeDataType</c> int. Descriptive metadata — the factory attaches it to the line snapshot without any
/// re-valuation and it never enters a sum or invariant.
/// </summary>
public sealed record LineAttributeSnapshotInput(
    string   DefinitionCode,
    int      DataType,
    string?  ValueLookupItemCode,
    string?  ValueLookupItemLabel,
    decimal? ValueNumber,
    string?  ValueText,
    bool?    ValueBool,
    int      SortOrder = 0);

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
