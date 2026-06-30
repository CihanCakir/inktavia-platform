namespace Aizen.Modules.Payment.Abstraction.Response;

/// <summary>
/// Returned by IPaymentGatewayProvider.HandleWebhookAsync after processing a webhook event.
/// IsSuccess=true means a successful payment was confirmed and the transaction should be captured.
/// </summary>
public sealed class PaymentApplyResult
{
    /// <summary>Gateway reference stored on the transaction (Iyzico checkoutToken / manual ref).</summary>
    public required string GatewayReference     { get; init; }

    /// <summary>true = payment succeeded and transaction should move to Captured status.</summary>
    public required bool   IsSuccess            { get; init; }

    public string?         ErrorMessage         { get; init; }

    /// <summary>Gross amount confirmed by the gateway (TRY). 0 if unknown.</summary>
    public decimal         PaidAmount           { get; init; }

    public string          CurrencyCode         { get; init; } = "TRY";

    /// <summary>Internal transaction ID linked to this gateway reference. Resolved from DB lookup.</summary>
    public long?           TransactionId        { get; init; }

    /// <summary>Iyzico payment transaction ID (different from our internal ID).</summary>
    public string?         GatewayTransactionId { get; init; }
}
