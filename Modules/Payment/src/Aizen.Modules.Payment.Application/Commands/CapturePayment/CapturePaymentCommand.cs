using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Commands.CapturePayment;

/// <summary>
/// Called when the payment gateway confirms the customer has paid (webhook confirmation).
/// Transitions the transaction from PendingIntent → Captured.
///
/// When called from a webhook (Iyzico flow), TransactionId is null — the handler
/// looks up the transaction by GatewayReference.
/// When called via admin (manual gateway), TransactionId can be provided directly.
/// </summary>
public sealed class CapturePaymentCommand : AizenCommand<bool>
{
    /// <summary>Optional — when null, transaction is looked up by GatewayReference.</summary>
    public long?  TransactionId    { get; init; }

    public required string GatewayReference { get; init; }
    public required decimal PaidAmount      { get; init; }
    public required string CurrencyCode     { get; init; }
    public string? AdminNote                { get; init; }
}
