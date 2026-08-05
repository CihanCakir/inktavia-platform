using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Money;

namespace Aizen.Modules.Payment.Domain.Entities.Economics;

/// <summary>
/// Immutable, insert-only economic ledger record (§5, §13.4) — the single canonical snapshot of a
/// transaction's economics, captured at the moment of acceptance (populated by P8's
/// <c>CalculateServiceRequestPaymentEconomics</c>). Once created it is never mutated: every property has
/// a <c>private set</c>, there is no <c>Update</c> method, and the only construction path is the
/// validating <see cref="Create"/> factory which enforces the §4 zero-tolerance invariants.
///
/// <para>KDV-aware money model (§13.3/§2): service and platform fee each carry net / vat / gross.
/// Commission and platform fee are rounded <b>separately</b> (§13.6 via <see cref="MoneyMath"/>);
/// <see cref="ProviderNetAmountSnapshot"/> is <b>derived</b> (ServiceAmount − CommissionAmount) and
/// <see cref="PlatformGrossShareSnapshot"/> is the platform's <b>book</b> share (Commission +
/// PlatformFeeGross = CustomerTotal − ProviderNet, §13.10) — <b>not</b> the actual bank-settled amount.</para>
///
/// <para><b>Extensibility (do NOT add now):</b> discount / funding (§19.6–19.7 → P6), commission-benefit
/// (§19.4–19.5 → P7), contribution / profit-protection (§19.12 → P5), settlement-net and refund-allocation
/// FKs (§13.10 → P10), and line-level snapshot tables (§20.15 → S8) are appended in later phases without
/// redesign. This record holds only the P1 core.</para>
/// </summary>
[DocumentationInfo("Payment economics snapshot entity",
    "Immutable, insert-only canonical economic ledger record. Constructed only via the validating Create " +
    "factory that enforces the §4 zero-tolerance invariants. Extensible for P5/P6/P7/P10/S8 fields.")]
public sealed class PaymentEconomicsSnapshotEntity : AizenEntityWithAudit
{
    // ── Identity / context ─────────────────────────────────────────────────────
    /// <summary>Human-readable unique code, e.g. "PES-YYYYMMDD-XXXX".</summary>
    public string                 SnapshotCode          { get; private set; } = default!;
    public TransactionContextType ContextType           { get; private set; }
    /// <summary>Offer / service-request id this snapshot describes.</summary>
    public long                   ContextId             { get; private set; }
    public string                 CurrencyCodeSnapshot  { get; private set; } = "TRY";
    /// <summary>Rounding policy captured for audit, e.g. "AwayFromZero-2".</summary>
    public string                 RoundingModeSnapshot  { get; private set; } = "AwayFromZero-2";
    public DateTime               CreatedAtUtc          { get; private set; }

    // ── Service (net / vat / gross) ────────────────────────────────────────────
    /// <summary>Net service amount; = commission base at P1.</summary>
    public decimal ServiceAmountSnapshot                { get; private set; }
    public decimal ServiceVatAmountSnapshot             { get; private set; }
    public decimal ServiceGrossAmountSnapshot           { get; private set; }
    /// <summary>Post-discount customer-payable service amount; = ServiceAmount at P1 (discounts arrive P6).</summary>
    public decimal CustomerPayableServiceAmountSnapshot { get; private set; }

    // ── Commission ─────────────────────────────────────────────────────────────
    /// <summary>= ServiceAmount at P1.</summary>
    public decimal CommissionBaseAmountSnapshot         { get; private set; }
    public decimal CommissionRateSnapshot               { get; private set; }
    public decimal CommissionAmountSnapshot             { get; private set; }
    /// <summary>Derived: ServiceAmount − CommissionAmount (not independently rounded).</summary>
    public decimal ProviderNetAmountSnapshot            { get; private set; }

    // ── Platform fee (net / vat / gross + resolved bounds) ─────────────────────
    /// <summary>= CustomerPayableServiceAmount (§19.13).</summary>
    public decimal PlatformFeeBaseAmountSnapshot        { get; private set; }
    /// <summary>The resolved PlatformFeeRule id (P3). Nullable at P1 since no rule engine exists yet.</summary>
    public long?   PlatformFeeRuleIdSnapshot            { get; private set; }
    public decimal PlatformFeeRateSnapshot              { get; private set; }
    public decimal PlatformFeeMinimumSnapshot           { get; private set; }
    public decimal PlatformFeeMaximumSnapshot           { get; private set; }
    public decimal PlatformFeeNetAmountSnapshot         { get; private set; }
    public decimal PlatformFeeVatAmountSnapshot         { get; private set; }
    public decimal PlatformFeeGrossAmountSnapshot       { get; private set; }

    // ── Totals (§13.10 — book share only) ──────────────────────────────────────
    public decimal CustomerTotalAmountSnapshot          { get; private set; }
    /// <summary>
    /// Platform's gross book share = Commission + PlatformFeeGross = CustomerTotal − ProviderNet (§13.10).
    /// This is the book figure only. The actual bank-settled amount (gross − processing/gateway expenses),
    /// <c>PlatformSettlementNetAmount</c>, is a settlement-side field added in P10 — it is deliberately NOT here.
    /// </summary>
    public decimal PlatformGrossShareSnapshot           { get; private set; }

    // ── §20.15 aggregate decomposition (BE-S8, append-only) — derived from line sums ──
    /// <summary>Σ line gross before discounts. Legacy (BE-P1) rows: 0.</summary>
    public decimal OriginalServiceGrossAmountSnapshot   { get; private set; }
    public decimal TotalCustomerDiscountSnapshot        { get; private set; }
    public decimal TotalProviderFundedDiscountSnapshot  { get; private set; }
    public decimal TotalPlatformFundedDiscountSnapshot  { get; private set; }
    /// <summary>Σ line VAT (= <see cref="ServiceVatAmountSnapshot"/> in the line-first path).</summary>
    public decimal ServiceVatTotalSnapshot              { get; private set; }

    // ── Line snapshot children (BE-S8) — immutable, insert-only (§20.15) ────────
    private readonly List<OfferLineEconomicsSnapshotEntity> _offerLines = new();
    public IReadOnlyCollection<OfferLineEconomicsSnapshotEntity> OfferLines => _offerLines.AsReadOnly();

    private readonly List<CommissionAllocationSnapshotEntity> _commissionAllocations = new();
    public IReadOnlyCollection<CommissionAllocationSnapshotEntity> CommissionAllocations => _commissionAllocations.AsReadOnly();

    private readonly List<DiscountAllocationSnapshotEntity> _discountAllocations = new();
    public IReadOnlyCollection<DiscountAllocationSnapshotEntity> DiscountAllocations => _discountAllocations.AsReadOnly();

    // P10: settlement / RefundAllocation FKs to this snapshot are added in their own phase — not now.
    // S4/S2: TravelPricingSnapshot / PricingAttributeSnapshot are RESERVED — not built here.

    private PaymentEconomicsSnapshotEntity() { }

    // ── Factory (only construction path — validates §4) ────────────────────────

    /// <summary>
    /// Builds a validated, immutable economics snapshot from caller-computed inputs (P8).
    /// Every money input is normalised through <see cref="MoneyMath.Round"/> (2-dp AwayFromZero) and the
    /// rate through <see cref="MoneyMath.RoundRate"/> (4-dp). <see cref="ProviderNetAmountSnapshot"/> and
    /// <see cref="PlatformGrossShareSnapshot"/> are <b>derived</b>, then all §4 invariants are checked with
    /// exact equality (zero tolerance). Throws <see cref="PaymentEconomicsInvariantException"/> on any mismatch.
    /// </summary>
    public static PaymentEconomicsSnapshotEntity Create(
        TransactionContextType contextType,
        long    contextId,
        // service
        decimal serviceAmount,
        decimal serviceVatAmount,
        decimal serviceGrossAmount,
        decimal customerPayableServiceAmount,
        // commission
        decimal commissionBaseAmount,
        decimal commissionRate,
        decimal commissionAmount,
        // platform fee
        long?   platformFeeRuleId,
        decimal platformFeeBaseAmount,
        decimal platformFeeRate,
        decimal platformFeeMinimum,
        decimal platformFeeMaximum,
        decimal platformFeeNetAmount,
        decimal platformFeeVatAmount,
        decimal platformFeeGrossAmount,
        // totals
        decimal customerTotalAmount,
        // meta
        string?   snapshotCode  = null,
        DateTime? createdAtUtc  = null,
        string    currencyCode  = "TRY")
    {
        var createdAt = createdAtUtc ?? DateTime.UtcNow;

        // ── Normalise via MoneyMath (§13.6) ─────────────────────────────────────
        serviceAmount                = MoneyMath.Round(serviceAmount);
        serviceVatAmount             = MoneyMath.Round(serviceVatAmount);
        serviceGrossAmount           = MoneyMath.Round(serviceGrossAmount);
        customerPayableServiceAmount = MoneyMath.Round(customerPayableServiceAmount);
        commissionBaseAmount         = MoneyMath.Round(commissionBaseAmount);
        commissionRate               = MoneyMath.RoundRate(commissionRate);
        commissionAmount             = MoneyMath.Round(commissionAmount);          // rounded separately (§2)
        platformFeeBaseAmount        = MoneyMath.Round(platformFeeBaseAmount);
        platformFeeRate              = MoneyMath.RoundRate(platformFeeRate);
        platformFeeMinimum           = MoneyMath.Round(platformFeeMinimum);
        platformFeeMaximum           = MoneyMath.Round(platformFeeMaximum);
        platformFeeNetAmount         = MoneyMath.Round(platformFeeNetAmount);
        platformFeeVatAmount         = MoneyMath.Round(platformFeeVatAmount);
        platformFeeGrossAmount       = MoneyMath.Round(platformFeeGrossAmount);    // rounded separately (§2)
        customerTotalAmount          = MoneyMath.Round(customerTotalAmount);

        // ── Derive (not independently rounded — difference of rounded values) ───
        var providerNetAmount   = serviceAmount - commissionAmount;                          // §2
        var platformGrossShare  = commissionAmount + platformFeeGrossAmount;                 // §13.10 (book)

        // ── Validate §4 invariants — ZERO tolerance (exact equality) ────────────
        Validate(
            serviceAmount, serviceVatAmount, serviceGrossAmount, customerPayableServiceAmount,
            commissionBaseAmount, commissionRate, commissionAmount, providerNetAmount,
            platformFeeNetAmount, platformFeeVatAmount, platformFeeGrossAmount,
            customerTotalAmount, platformGrossShare);

        return new PaymentEconomicsSnapshotEntity
        {
            SnapshotCode                         = snapshotCode ?? GenerateSnapshotCode(createdAt),
            ContextType                          = contextType,
            ContextId                            = contextId,
            CurrencyCodeSnapshot                 = currencyCode.ToUpperInvariant(),
            RoundingModeSnapshot                 = "AwayFromZero-2",
            CreatedAtUtc                         = createdAt,

            ServiceAmountSnapshot                = serviceAmount,
            ServiceVatAmountSnapshot             = serviceVatAmount,
            ServiceGrossAmountSnapshot           = serviceGrossAmount,
            CustomerPayableServiceAmountSnapshot = customerPayableServiceAmount,

            CommissionBaseAmountSnapshot         = commissionBaseAmount,
            CommissionRateSnapshot               = commissionRate,
            CommissionAmountSnapshot             = commissionAmount,
            ProviderNetAmountSnapshot            = providerNetAmount,

            PlatformFeeBaseAmountSnapshot        = platformFeeBaseAmount,
            PlatformFeeRuleIdSnapshot            = platformFeeRuleId,
            PlatformFeeRateSnapshot              = platformFeeRate,
            PlatformFeeMinimumSnapshot           = platformFeeMinimum,
            PlatformFeeMaximumSnapshot           = platformFeeMaximum,
            PlatformFeeNetAmountSnapshot         = platformFeeNetAmount,
            PlatformFeeVatAmountSnapshot         = platformFeeVatAmount,
            PlatformFeeGrossAmountSnapshot       = platformFeeGrossAmount,

            CustomerTotalAmountSnapshot          = customerTotalAmount,
            PlatformGrossShareSnapshot           = platformGrossShare,

            IsActive                             = true,
        };
    }

    /// <summary>§4 invariants — exact equality on rounded decimals, no epsilon.</summary>
    private static void Validate(
        decimal serviceAmount, decimal serviceVatAmount, decimal serviceGrossAmount,
        decimal customerPayableServiceAmount,
        decimal commissionBaseAmount, decimal commissionRate, decimal commissionAmount,
        decimal providerNetAmount,
        decimal platformFeeNetAmount, decimal platformFeeVatAmount, decimal platformFeeGrossAmount,
        decimal customerTotalAmount, decimal platformGrossShare)
    {
        if (providerNetAmount + platformGrossShare != customerTotalAmount)
            throw new PaymentEconomicsInvariantException(
                "ProviderNetAmount + PlatformGrossShare == CustomerTotalAmount");

        if (customerTotalAmount < providerNetAmount)
            throw new PaymentEconomicsInvariantException(
                "CustomerTotalAmount >= ProviderNetAmount");

        if (platformFeeGrossAmount != platformFeeNetAmount + platformFeeVatAmount)
            throw new PaymentEconomicsInvariantException(
                "PlatformFeeGrossAmount == PlatformFeeNetAmount + PlatformFeeVatAmount");

        if (serviceGrossAmount != serviceAmount + serviceVatAmount)
            throw new PaymentEconomicsInvariantException(
                "ServiceGrossAmount == ServiceAmount + ServiceVatAmount");

        if (commissionAmount != MoneyMath.Round(commissionBaseAmount * commissionRate))
            throw new PaymentEconomicsInvariantException(
                "CommissionAmount == MoneyMath.Round(CommissionBaseAmount * CommissionRate)");

        if (providerNetAmount != serviceAmount - commissionAmount)
            throw new PaymentEconomicsInvariantException(
                "ProviderNetAmount == ServiceAmount - CommissionAmount");

        if (customerTotalAmount != customerPayableServiceAmount + platformFeeGrossAmount)
            throw new PaymentEconomicsInvariantException(
                "CustomerTotalAmount == CustomerPayableServiceAmount + PlatformFeeGrossAmount");
    }

    // ── Line-first factory (BE-S8, §20.15) — the heart ─────────────────────────

    /// <summary>
    /// Builds the immutable aggregate snapshot <b>from its line snapshots</b> (§20.15). Every aggregate total is
    /// <b>derived as a line sum</b> (never a passed-in blended number); the 8 §20.15 equalities are enforced with ZERO
    /// tolerance (throws <see cref="PaymentEconomicsInvariantException"/> naming the failed relationship). Per-line
    /// commission rounding (from S7) is carried through unchanged and the aggregate commission is the <b>sum of the
    /// rounded line commissions</b> — NOT <c>Round(ΣBase × blendedRate)</c>.
    ///
    /// <para><b>BE-P1 reconciliation:</b> the aggregate <see cref="CommissionRateSnapshot"/> is a <b>reporting-only</b>
    /// effective rate (<c>= CommissionAmount / CommissionBase</c>, 4-dp) and the <b>binding</b> commission invariant is
    /// the line-sum (<c>CommissionAmount == Σ line commission</c>) — the one BE-P1 invariant this path reconciles.
    /// BE-P1's original <see cref="Create"/> (blended path) is untouched.</para>
    ///
    /// <para>Discounts are 0 in the narrow core (S6 populates funding → DiscountAllocation rows). The result carries its
    /// OfferLine / CommissionAllocation children as one immutable unit.</para>
    /// </summary>
    public static PaymentEconomicsSnapshotEntity CreateFromLines(
        long                     contextId,
        string                   currencyCode,
        IReadOnlyList<LineEconomicsInput> lines,
        PlatformFeeInput         platformFee,
        decimal                  customerPayableServiceAmount,
        TransactionContextType   contextType = TransactionContextType.ServiceRequest,
        string?                  snapshotCode = null,
        DateTime?                createdAtUtc = null)
    {
        if (lines is null || lines.Count == 0)
            throw new PaymentEconomicsInvariantException("CreateFromLines requires at least one line.");

        var createdAt = createdAtUtc ?? DateTime.UtcNow;

        // ── Platform fee + customer-payable normalisation (§13.6) ──────────────
        var feeBase = MoneyMath.Round(platformFee.Base);
        var feeRate = MoneyMath.RoundRate(platformFee.Rate);
        var feeMin  = MoneyMath.Round(platformFee.Minimum);
        var feeMax  = MoneyMath.Round(platformFee.Maximum);
        var feeNet  = MoneyMath.Round(platformFee.Net);
        var feeVat  = MoneyMath.Round(platformFee.Vat);
        var feeGross = MoneyMath.Round(platformFee.Gross);
        var customerPayable = MoneyMath.Round(customerPayableServiceAmount);

        // ── Per-line: normalise, validate internal consistency, accumulate sums ─
        decimal sumGross = 0, sumCustDisc = 0, sumProvDisc = 0, sumPlatDisc = 0,
                sumCommission = 0, sumProviderNet = 0, sumVat = 0, sumLineTotal = 0, sumCommissionBase = 0;

        var offerLines            = new List<OfferLineEconomicsSnapshotEntity>(lines.Count);
        var commissionAllocations = new List<CommissionAllocationSnapshotEntity>(lines.Count);
        var discountAllocations   = new List<DiscountAllocationSnapshotEntity>();

        foreach (var l in lines)
        {
            var gross = MoneyMath.Round(l.GrossBeforeDiscount);
            var dc    = MoneyMath.Round(l.CustomerDiscount);
            var dp    = MoneyMath.Round(l.ProviderFundedDiscount);
            var dpl   = MoneyMath.Round(l.PlatformFundedDiscount);
            var cb    = MoneyMath.Round(l.CommissionBase);
            var cr    = MoneyMath.RoundRate(l.CommissionRate);
            var ca    = MoneyMath.Round(l.CommissionAmount);
            var pn    = MoneyMath.Round(l.ProviderNet);
            var vat   = MoneyMath.Round(l.LineVat);
            var lt    = MoneyMath.Round(l.LineTotal);

            // BE-P8b: CustomerDiscount (dc) is the customer's post-discount reduction; ProviderFunded (dp) + PlatformFunded
            // (dpl) are its FUNDING SPLIT (who bears it), NOT additional reductions. So the base is reduced by dc once, and
            // the funding must sum to it (§19.6): dc == dp + dpl (+ supplier, 0 in the narrow core). Zero-tolerance.
            if (dc != dp + dpl)
                throw new PaymentEconomicsInvariantException(
                    $"Σ(line funding) == CustomerDiscount [line {l.LineRef}: platform {dpl} + provider {dp} != customerDiscount {dc}]");

            var netFromGross = gross - dc;   // post-discount net (customer discount reduces the base once)

            // Line commission (§13.6 per-line rounding): commissionable ⇒ Round(base×rate); exempt ⇒ 0.
            var expectedCommission = l.Commissionable ? MoneyMath.Round(cb * cr) : 0m;
            if (ca != expectedCommission)
                throw new PaymentEconomicsInvariantException(
                    $"Σ(line commission amounts) == TotalProviderCommission [line {l.LineRef}: commission {ca} != {expectedCommission}]");

            // Line provider net = net − commission.
            if (pn != netFromGross - ca)
                throw new PaymentEconomicsInvariantException(
                    $"Σ(line provider net amounts) == ProviderNetTotal [line {l.LineRef}: providerNet {pn} != {netFromGross - ca}]");

            // Line gross ↔ total/vat reconciliation: (gross − discounts) + vat == lineTotal.
            if (netFromGross + vat != lt)
                throw new PaymentEconomicsInvariantException(
                    $"Σ(line gross/VAT) reconciliation [line {l.LineRef}: net {netFromGross} + vat {vat} != lineTotal {lt}]");

            sumGross          += gross;
            sumCustDisc       += dc;
            sumProvDisc       += dp;
            sumPlatDisc       += dpl;
            sumCommission     += ca;
            sumProviderNet    += pn;
            sumVat            += vat;
            sumLineTotal      += lt;
            sumCommissionBase += cb;

            // S2d — snapshot the line's pricing attributes (descriptive; no re-valuation, not in any sum/invariant).
            List<OfferLineAttributeSnapshotEntity>? attributeSnapshots = null;
            if (l.Attributes is { Count: > 0 })
                attributeSnapshots = l.Attributes
                    .Select(a => OfferLineAttributeSnapshotEntity.Create(
                        a.DefinitionCode, a.DataType, a.ValueLookupItemCode, a.ValueLookupItemLabel,
                        a.ValueNumber, a.ValueText, a.ValueBool, a.SortOrder))
                    .ToList();

            offerLines.Add(OfferLineEconomicsSnapshotEntity.Create(
                l.LineRef, l.ItemType, l.PricingMethod, gross, dc, dp, dpl, l.CommissionEligibility,
                cb, cr, ca, pn, vat, lt, currencyCode, l.SortOrder, attributeSnapshots,
                l.Fx));   // S3 — frozen FX metadata folded onto the line snapshot (tamper-checked in the factory)

            commissionAllocations.Add(CommissionAllocationSnapshotEntity.Create(
                l.LineRef, l.RuleId, l.RuleCode, cb, cr, ca, l.Commissionable));

            // Discount allocations: only when a discount is actually funded (narrow core = none).
            if (dp > 0m) discountAllocations.Add(DiscountAllocationSnapshotEntity.Create(l.LineRef, CustomerDiscountFundingMode.ProviderFunded, dp, null));
            if (dpl > 0m) discountAllocations.Add(DiscountAllocationSnapshotEntity.Create(l.LineRef, CustomerDiscountFundingMode.PlatformFunded, dpl, null));
        }

        // ── Aggregates derived ONLY from line sums (§3) ─────────────────────────
        var serviceNet   = sumLineTotal - sumVat;                 // post-discount net service
        var serviceGross = serviceNet + sumVat;                   // = ΣLineTotal
        var commissionAmount = sumCommission;                     // line-sum (binding), NOT a blended re-round
        var providerNet  = sumProviderNet;
        var customerTotal    = customerPayable + feeGross;
        var platformGrossShare = customerTotal - providerNet;     // §13.10 (customer total − provider net)
        var reportingRate = sumCommissionBase > 0m ? MoneyMath.RoundRate(commissionAmount / sumCommissionBase) : 0m;

        // ── 8 equalities + reconciled BE-P1 aggregate invariants (zero tolerance) ─
        if (sumLineTotal + feeGross != customerTotal)
            throw new PaymentEconomicsInvariantException(
                $"Σ(line totals) + PlatformFeeGross == CustomerTotalAmount [{sumLineTotal} + {feeGross} != {customerTotal}]");
        if (customerTotal != customerPayable + feeGross)
            throw new PaymentEconomicsInvariantException("CustomerTotalAmount == CustomerPayableServiceAmount + PlatformFeeGrossAmount");
        if (feeGross != feeNet + feeVat)
            throw new PaymentEconomicsInvariantException("PlatformFeeGrossAmount == PlatformFeeNetAmount + PlatformFeeVatAmount");
        if (serviceGross != serviceNet + sumVat)
            throw new PaymentEconomicsInvariantException("ServiceGrossAmount == ServiceAmount + ServiceVatAmount");
        if (providerNet + platformGrossShare != customerTotal)
            throw new PaymentEconomicsInvariantException("ProviderNetAmount + PlatformGrossShare == CustomerTotalAmount");
        if (customerTotal < providerNet)
            throw new PaymentEconomicsInvariantException("CustomerTotalAmount >= ProviderNetAmount");
        if (providerNet != serviceNet - commissionAmount)
            throw new PaymentEconomicsInvariantException("ProviderNetAmount == ServiceAmount - CommissionAmount");

        var snapshot = new PaymentEconomicsSnapshotEntity
        {
            SnapshotCode                         = snapshotCode ?? GenerateSnapshotCode(createdAt),
            ContextType                          = contextType,
            ContextId                            = contextId,
            CurrencyCodeSnapshot                 = currencyCode.ToUpperInvariant(),
            RoundingModeSnapshot                 = "AwayFromZero-2",
            CreatedAtUtc                         = createdAt,

            ServiceAmountSnapshot                = serviceNet,
            ServiceVatAmountSnapshot             = sumVat,
            ServiceGrossAmountSnapshot           = serviceGross,
            CustomerPayableServiceAmountSnapshot = customerPayable,

            CommissionBaseAmountSnapshot         = sumCommissionBase,
            CommissionRateSnapshot               = reportingRate,      // reporting-only effective rate
            CommissionAmountSnapshot             = commissionAmount,   // = Σ line commission (binding)
            ProviderNetAmountSnapshot            = providerNet,

            PlatformFeeBaseAmountSnapshot        = feeBase,
            PlatformFeeRuleIdSnapshot            = platformFee.RuleId,
            PlatformFeeRateSnapshot              = feeRate,
            PlatformFeeMinimumSnapshot           = feeMin,
            PlatformFeeMaximumSnapshot           = feeMax,
            PlatformFeeNetAmountSnapshot         = feeNet,
            PlatformFeeVatAmountSnapshot         = feeVat,
            PlatformFeeGrossAmountSnapshot       = feeGross,

            CustomerTotalAmountSnapshot          = customerTotal,
            PlatformGrossShareSnapshot           = platformGrossShare,

            OriginalServiceGrossAmountSnapshot   = sumGross,
            TotalCustomerDiscountSnapshot        = sumCustDisc,
            TotalProviderFundedDiscountSnapshot  = sumProvDisc,
            TotalPlatformFundedDiscountSnapshot  = sumPlatDisc,
            ServiceVatTotalSnapshot              = sumVat,

            IsActive                             = true,
        };

        snapshot._offerLines.AddRange(offerLines);
        snapshot._commissionAllocations.AddRange(commissionAllocations);
        snapshot._discountAllocations.AddRange(discountAllocations);
        return snapshot;
    }

    /// <summary>Generates a "PES-YYYYMMDD-XXXX" code. Uniqueness is enforced by the DB unique index.</summary>
    public static string GenerateSnapshotCode(DateTime atUtc)
    {
        var suffix = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        return $"PES-{atUtc:yyyyMMdd}-{suffix}";
    }
}
