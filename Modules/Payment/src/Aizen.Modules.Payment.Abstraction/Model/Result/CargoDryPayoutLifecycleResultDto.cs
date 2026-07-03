using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Model.Result;

/// <summary>
/// Result DTO returned by every ICargoDrySettlementPayoutLifecycleService operation.
/// Carries the updated payout record state plus a human-readable message.
/// Phase 4D (July 2026).
/// </summary>
public sealed class CargoDryPayoutLifecycleResultDto
{
    public long           PayoutRecordId         { get; init; }
    public PayoutStatus   PayoutStatus            { get; init; }
    public long           ProviderProfileId       { get; init; }
    public decimal        Amount                  { get; init; }
    public string         CurrencyCode            { get; init; } = default!;
    public string?        ExternalReference       { get; init; }   // GatewayPayoutId / ManualPaymentReference
    public DateTime?      ApprovedAtUtc           { get; init; }
    public long?          ApprovedByUserId        { get; init; }
    public DateTime?      ProcessingAtUtc         { get; init; }
    public DateTime?      CompletedAtUtc          { get; init; }   // maps to ProcessedAt when Completed
    public long?          CompletedByUserId       { get; init; }
    public DateTime?      FailedAtUtc             { get; init; }
    public long?          FailedByUserId          { get; init; }
    public string?        FailureReason           { get; init; }
    public string?        AdminNote               { get; init; }
    public bool           AlreadyCompleted        { get; init; }   // idempotency signal on CompleteManual
    public string         Message                 { get; init; } = default!;
}
