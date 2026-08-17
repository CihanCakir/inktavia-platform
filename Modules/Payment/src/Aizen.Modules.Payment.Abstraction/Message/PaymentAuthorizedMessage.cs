using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// "Ödeme Provizyonda" — published when a payment is <b>authorized</b> (PreAuth / authorization hold) but not yet
/// captured. LATENT: BE-P9 PreAuth mode is scaffolded (see <see cref="PaymentAuthMode"/>) but the authorize-only
/// gateway flow + "Authorized/Held" transaction state that would publish this event are not built yet. Defined now so
/// the notification path is ready; the Notification module's PaymentAuthorizedConsumer consumes it (Payments category,
/// inherits N-B gating). Do NOT publish this until P9 delivers the authorize/capture split.
/// </summary>
public sealed class PaymentAuthorizedMessage : AizenBaseMessage
{
    public long   TransactionId    { get; init; }
    public string TransactionCode  { get; init; } = default!;
    public string GatewayReference { get; init; } = default!;

    public TransactionContextType ContextType  { get; init; }
    public long                   ContextId    { get; init; }
    public long?                  ContextSubId { get; init; }

    public long PayerProfileId { get; init; }

    public decimal AuthorizedAmount { get; init; }
    public string  CurrencyCode     { get; init; } = "TRY";

    public DateTime  AuthorizedAtUtc { get; init; }
    /// <summary>When the authorization hold expires (gateway-defined, e.g. iyzico ~25 days). Nullable until P9 sets it.</summary>
    public DateTime? HoldExpiresAtUtc { get; init; }
}
