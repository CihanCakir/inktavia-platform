using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// Published after an invoice transitions from Draft → Issued.
/// Consumers: Notification module (buyer email / PDF delivery), BFF cache invalidation.
///
/// Idempotency: <see cref="InvoiceId"/> is globally unique per invoice.
/// Consumers should deduplicate on INVOICE-ISSUED-{InvoiceId}.
/// </summary>
public sealed class InvoiceIssuedMessage : AizenBaseMessage
{
    public long          InvoiceId    { get; init; }
    public string        InvoiceNumber { get; init; } = default!;
    public InvoiceType   InvoiceType  { get; init; }

    /// <summary>Cross-module reference to the buyer (Identity profile Id). May be null for anonymous invoices.</summary>
    public long?   BuyerUserId  { get; init; }
    public string  BuyerName    { get; init; } = default!;

    /// <summary>Null = Inktavia is the seller (platform/commission invoices).</summary>
    public long?   SellerUserId { get; init; }

    public decimal TotalAmount  { get; init; }
    public string  Currency     { get; init; } = "TRY";
    public DateTime IssuedAtUtc { get; init; }

    /// <summary>The domain entity that triggered invoice creation (ServiceRequestId, SubscriptionId, etc.).</summary>
    public long?             SourceId   { get; init; }
    public InvoiceSourceType SourceType { get; init; }
}
