using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.CreatePaymentEscrow;

public sealed class CreatePaymentEscrowCommand : AizenCommand<CreatePaymentEscrowResult>
{
    /// <summary>Globally unique key — prevents duplicate charges on retry.</summary>
    public required string IdempotencyKey { get; init; }
    public required TransactionContext Context { get; init; }
    public required TransactionType TransactionType { get; init; }
    public required long  PayerProfileId     { get; init; }
    /// <summary>
    /// Recipient profile (provider) for SR payments. Null for platform-collected payments
    /// such as CargoDryRenewal and ProviderSubscription where funds go to the platform.
    /// </summary>
    public long? RecipientProfileId { get; init; }
    public required decimal GrossAmount { get; init; }
    public required decimal DiscountAmount { get; init; }
    public required string CurrencyCode { get; init; }
    /// <summary>Nullable — resolved from active subscription at command time.</summary>
    public long? ProviderPlanId { get; init; }
    public string? CategoryCode { get; init; }
    /// <summary>true = hold until SR completion; false = immediate settle.</summary>
    public bool EscrowRequired { get; init; } = true;

    /// <summary>
    /// PrincipalSale / platform-collected (CargoDry supply, additive): when true the platform is the sole merchant —
    /// NO provider split. Commission calc is skipped and the transaction is persisted with CommissionAmount=0,
    /// CommissionRate=0, NetPayoutAmount=0 (the whole gross is platform revenue). The provider (if any) is compensated
    /// out-of-band via the CargoDry sell-through settlement, never via this escrow. Guarantees a zero provider split
    /// by construction. Default false preserves the existing marketplace behaviour byte-for-byte.
    /// </summary>
    public bool PlatformCollectedNoProviderShare { get; init; } = false;
}
