using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// N3-B — published when a gateway chargeback is recorded (BE-P10 §21.2), after persistence and only on the fresh
/// path (a duplicate gateway reference is idempotent and publishes nothing). Lets the Notification module alert the
/// provider (whose transaction was charged back → P10 clawback / negative-balance impact) and admins.
/// </summary>
public sealed class PaymentChargebackRecordedMessage : AizenBaseMessage
{
    public long   TransactionId   { get; init; }
    public string TransactionCode { get; init; } = default!;

    public TransactionContextType ContextType { get; init; }
    public long                   ContextId   { get; init; }
    public long?                  ContextSubId { get; init; }

    /// <summary>The provider whose transaction was charged back (transaction RecipientProfileId; 0 if none).</summary>
    public long ProviderProfileId { get; init; }
    /// <summary>The payer/owner (transaction PayerProfileId).</summary>
    public long PayerProfileId    { get; init; }

    public decimal Amount       { get; init; }
    public string  CurrencyCode { get; init; } = "TRY";

    public string   GatewayChargebackReference { get; init; } = default!;
    public DateTime ReceivedAtUtc              { get; init; }
}
