using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Economics;

/// <summary>
/// Immutable, insert-only per-line discount funding allocation (§20.15) — child of the aggregate
/// <see cref="PaymentEconomicsSnapshotEntity"/> (FK, OnDelete Restrict). <b>Modeled now, rows only when S6 provides
/// discounts</b> — the narrow core has no discounts, so no rows are created and the discount totals are 0. No mutators.
/// </summary>
[DocumentationInfo("Discount allocation snapshot entity",
    "Immutable per-line discount funding allocation (Platform/Provider/Shared/Supplier). Modeled in S8; populated by S6.")]
public sealed class DiscountAllocationSnapshotEntity : AizenEntityWithAudit
{
    public long                        EconomicsSnapshotId { get; private set; }
    public string                      LineRef             { get; private set; } = default!;
    public CustomerDiscountFundingMode FundingSource       { get; private set; }
    public decimal                     DiscountAmount      { get; private set; }
    public string?                     RuleCode            { get; private set; }

    private DiscountAllocationSnapshotEntity() { }

    internal static DiscountAllocationSnapshotEntity Create(
        string lineRef, CustomerDiscountFundingMode fundingSource, decimal discountAmount, string? ruleCode)
        => new()
        {
            LineRef        = lineRef,
            FundingSource  = fundingSource,
            DiscountAmount = discountAmount,
            RuleCode       = ruleCode,
            IsActive       = true,
        };
}
