using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// Published when escrowed funds are released to the provider (SR completion approval).
/// Triggers provider payout record creation and Notification module alerts.
/// </summary>
public sealed class PaymentEscrowReleasedMessage : AizenBaseMessage
{
    public long   TransactionId       { get; init; }
    public string TransactionCode     { get; init; } = default!;

    public TransactionContextType ContextType  { get; init; }
    public long                   ContextId    { get; init; }   // ServiceRequestId
    public long?                  ContextSubId { get; init; }   // OfferId

    public long    RecipientProfileId { get; init; }             // Provider profile
    public long    PayerProfileId     { get; init; }

    public decimal NetPayoutAmount    { get; init; }
    public decimal CommissionAmount   { get; init; }
    public string  CurrencyCode       { get; init; } = "TRY";

    public string? GatewayPayoutId    { get; init; }             // Iyzico paymentTransactionId
    public DateTime ReleasedAtUtc     { get; init; }
}
