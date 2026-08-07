using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.RefundAllocation;

/// <summary>
/// BE-P10 §7.4 — the provider negative-balance ledger. <see cref="Balance"/> may go negative when a release-after refund /
/// chargeback claws back settled funds. A new payout offsets the negative balance FIRST; exceeding
/// <see cref="NegativeBalanceLimit"/> blocks payout + offer acceptance. Optimistic-concurrency via <see cref="Version"/>.
/// </summary>
[DocumentationInfo("Provider balance entity",
    "Per-provider negative-balance ledger: refund/chargeback clawbacks push it negative; payouts offset it first; a limit blocks payout/acceptance.")]
[NoMessagebusSync] // domain-authored P10 provider balance (optimistic-versioned) — never generically writable
public sealed class ProviderBalanceEntity : AizenEntityWithAudit
{
    public long    ProviderProfileId    { get; private set; }
    public string  CurrencyCode         { get; private set; } = "TRY";
    /// <summary>Signed running balance. Negative = the provider owes the platform.</summary>
    public decimal Balance              { get; private set; }
    public decimal NegativeBalanceLimit { get; private set; }   // 0 = no limit
    public long    Version              { get; private set; }

    private readonly List<ProviderBalanceMovementEntity> _movements = new();
    public IReadOnlyCollection<ProviderBalanceMovementEntity> Movements => _movements.AsReadOnly();

    public decimal NegativeAmount => Balance < 0m ? -Balance : 0m;

    private ProviderBalanceEntity() { }

    public static ProviderBalanceEntity Create(long providerProfileId, string currencyCode, decimal negativeBalanceLimit)
        => new()
        {
            ProviderProfileId    = providerProfileId,
            CurrencyCode         = currencyCode.ToUpperInvariant(),
            Balance              = 0m,
            NegativeBalanceLimit = negativeBalanceLimit < 0m ? 0m : negativeBalanceLimit,
            IsActive             = true,
        };

    /// <summary>Clawback the un-recovered refund/chargeback portion — pushes the balance negative.</summary>
    public ProviderBalanceMovementEntity Clawback(decimal amount, ProviderBalanceMovementType type, long? refundRecordId, long? chargebackRecordId, string? note, DateTime atUtc)
    {
        if (amount <= 0m) throw new ArgumentException("Clawback amount must be positive.", nameof(amount));
        Balance -= amount;
        Version++;
        var m = ProviderBalanceMovementEntity.Create(Id, type, -amount, Balance, refundRecordId, chargebackRecordId, null, note, atUtc);
        _movements.Add(m);
        return m;
    }

    /// <summary>Offset the negative balance from an available payout amount (§7.3 step 3). Returns the amount applied.</summary>
    public decimal OffsetFromPayout(decimal availablePayout, DateTime atUtc)
    {
        if (availablePayout <= 0m || Balance >= 0m) return 0m;
        var offset = Math.Min(availablePayout, NegativeAmount);
        Balance += offset;
        Version++;
        _movements.Add(ProviderBalanceMovementEntity.Create(Id, ProviderBalanceMovementType.PayoutOffset, offset, Balance, null, null, null, "Payout offset", atUtc));
        return offset;
    }

    public void ManualAdjust(decimal signedAmount, long adminUserId, string note, DateTime atUtc)
    {
        Balance += signedAmount;
        Version++;
        _movements.Add(ProviderBalanceMovementEntity.Create(Id, ProviderBalanceMovementType.ManualAdjustment, signedAmount, Balance, null, null, adminUserId, note, atUtc));
    }

    /// <summary>§7.4 — the negative balance is over the configured limit (blocks payout + acceptance).</summary>
    public bool IsOverLimit() => NegativeBalanceLimit > 0m && NegativeAmount > NegativeBalanceLimit;

    public void SetNegativeBalanceLimit(decimal limit) => NegativeBalanceLimit = limit < 0m ? 0m : limit;
}

/// <summary>BE-P10 §7.4 — an audited movement on the provider balance ledger.</summary>
[NoMessagebusSync] // domain-authored immutable P10 provider balance ledger movement — never generically writable
public sealed class ProviderBalanceMovementEntity : AizenEntityWithAudit
{
    public long                        ProviderBalanceId  { get; private set; }
    public ProviderBalanceMovementType MovementType       { get; private set; }
    /// <summary>Signed delta applied to the balance (negative = clawback, positive = offset/credit).</summary>
    public decimal                     Amount             { get; private set; }
    public decimal                     BalanceAfter       { get; private set; }
    public long?                       RefundRecordId     { get; private set; }
    public long?                       ChargebackRecordId { get; private set; }
    public long?                       AdminUserId        { get; private set; }
    public string?                     Note               { get; private set; }
    public DateTime                    OccurredAtUtc      { get; private set; }

    private ProviderBalanceMovementEntity() { }

    internal static ProviderBalanceMovementEntity Create(
        long providerBalanceId, ProviderBalanceMovementType type, decimal amount, decimal balanceAfter,
        long? refundRecordId, long? chargebackRecordId, long? adminUserId, string? note, DateTime atUtc)
        => new()
        {
            ProviderBalanceId  = providerBalanceId,
            MovementType       = type,
            Amount             = amount,
            BalanceAfter       = balanceAfter,
            RefundRecordId     = refundRecordId,
            ChargebackRecordId = chargebackRecordId,
            AdminUserId        = adminUserId,
            Note               = note,
            OccurredAtUtc      = atUtc,
            IsActive           = true,
        };
}
