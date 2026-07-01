using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Payout;

[DocumentationInfo("Payout record entity",
    "Tracks a pending or completed provider payout. In manual mode, admin confirms; in Iyzico mode, transfer is automatic.")]
public sealed class PayoutRecordEntity : AizenEntityWithAudit
{
    public long         ProviderProfileId       { get; private set; }
    public long         PaymentTransactionId    { get; private set; }
    public decimal      Amount                  { get; private set; }
    public string       CurrencyCode            { get; private set; } = "TRY";
    public PayoutStatus Status                  { get; private set; }
    public string       GatewayProvider         { get; private set; } = default!;
    public string?      GatewayPayoutId         { get; private set; }  // Iyzico transfer reference
    public DateTime     RequestedAt             { get; private set; }
    public DateTime?    ProcessedAt             { get; private set; }
    public DateTime?    HeldAt                  { get; private set; }
    public string?      HoldReason              { get; private set; }
    public string?      FailureReason           { get; private set; }
    public string?      AdminNote               { get; private set; }

    private PayoutRecordEntity() { }

    public static PayoutRecordEntity Create(
        long providerProfileId, long paymentTransactionId,
        decimal amount, string currencyCode, string gatewayProvider)
    {
        return new PayoutRecordEntity
        {
            ProviderProfileId    = providerProfileId,
            PaymentTransactionId = paymentTransactionId,
            Amount               = amount,
            CurrencyCode         = currencyCode.ToUpperInvariant(),
            Status               = PayoutStatus.Pending,
            GatewayProvider      = gatewayProvider,
            RequestedAt          = DateTime.UtcNow,
            IsActive             = true,
        };
    }

    public void MarkCompleted(string? gatewayPayoutId, string? adminNote = null)
    {
        Status          = PayoutStatus.Completed;
        GatewayPayoutId = gatewayPayoutId;
        ProcessedAt     = DateTime.UtcNow;
        AdminNote       = adminNote;
    }

    public void MarkFailed(string reason)
    {
        Status        = PayoutStatus.Failed;
        FailureReason = reason;
        ProcessedAt   = DateTime.UtcNow;
    }

    public void MarkProcessing() => Status = PayoutStatus.Processing;

    /// <summary>
    /// Puts a Pending or Processing payout on administrative hold.
    /// Use ApproveManualPayout() to release from hold.
    /// </summary>
    public void Hold(string reason, string? adminNote = null)
    {
        Status     = PayoutStatus.OnHold;
        HoldReason = reason;
        HeldAt     = DateTime.UtcNow;
        AdminNote  = adminNote;
    }

    /// <summary>
    /// Releases a held payout and marks it as completed via manual disbursement.
    /// </summary>
    public void ApproveManualPayout(string gatewayPayoutId, string? adminNote = null)
    {
        Status          = PayoutStatus.Completed;
        GatewayPayoutId = gatewayPayoutId;
        ProcessedAt     = DateTime.UtcNow;
        AdminNote       = adminNote ?? AdminNote;
    }
}
