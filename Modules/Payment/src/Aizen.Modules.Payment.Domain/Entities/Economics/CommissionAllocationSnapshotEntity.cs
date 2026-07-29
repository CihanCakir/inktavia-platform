using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;

namespace Aizen.Modules.Payment.Domain.Entities.Economics;

/// <summary>
/// Immutable, insert-only per-line commission-rule audit trail (§20.15) — the resolved rule from S7 recorded against the
/// aggregate <see cref="PaymentEconomicsSnapshotEntity"/> (FK, OnDelete Restrict). No mutators.
/// </summary>
[DocumentationInfo("Commission allocation snapshot entity",
    "Immutable per-line commission-rule audit (rule id/code, base, resolved rate, amount, commissionable). Child of PaymentEconomicsSnapshot.")]
public sealed class CommissionAllocationSnapshotEntity : AizenEntityWithAudit
{
    public long    EconomicsSnapshotId  { get; private set; }
    public string  LineRef              { get; private set; } = default!;
    public long?   CommissionRuleId     { get; private set; }
    public string? CommissionRuleCode   { get; private set; }
    public decimal CommissionBaseAmount { get; private set; }
    public decimal ResolvedRate         { get; private set; }
    public decimal CommissionAmount     { get; private set; }
    public bool    Commissionable       { get; private set; }

    private CommissionAllocationSnapshotEntity() { }

    internal static CommissionAllocationSnapshotEntity Create(
        string lineRef, long? ruleId, string? ruleCode,
        decimal commissionBase, decimal resolvedRate, decimal commissionAmount, bool commissionable)
        => new()
        {
            LineRef              = lineRef,
            CommissionRuleId     = ruleId,
            CommissionRuleCode   = ruleCode,
            CommissionBaseAmount = commissionBase,
            ResolvedRate         = resolvedRate,
            CommissionAmount     = commissionAmount,
            Commissionable       = commissionable,
            IsActive             = true,
        };
}
