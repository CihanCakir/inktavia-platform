using Aizen.Core.Messagebus.Abstraction.Messages;
namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// Published when a PayoutRecord transitions to Completed.
/// Consumers: Notification module (notify provider), Reporting module (post-MVP).
/// </summary>
public sealed class PayoutCompletedMessage : AizenBaseMessage
{
    public long   PayoutRecordId      { get; init; }
    public long   TransactionId       { get; init; }
    public long   ProviderProfileId   { get; init; }

    public decimal Amount             { get; init; }
    public string  CurrencyCode       { get; init; } = "TRY";

    public string  GatewayProvider    { get; init; } = default!;
    public string? GatewayPayoutId    { get; init; }

    public DateTime ProcessedAtUtc    { get; init; }
}
