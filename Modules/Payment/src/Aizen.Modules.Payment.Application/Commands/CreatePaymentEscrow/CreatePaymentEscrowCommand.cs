using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model;

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
}

public sealed record CreatePaymentEscrowResult(
    long TransactionId,
    string TransactionCode,
    string GatewayReference,
    decimal CommissionRate,
    decimal CommissionAmount,
    decimal NetPayoutAmount
);
