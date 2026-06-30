using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Queries.GetPendingPayouts;

public sealed class GetPendingPayoutsQuery : AizenQuery<List<PendingPayoutDto>> { }

public sealed record PendingPayoutDto(
    long   PayoutRecordId,
    long   TransactionId,
    long   ProviderProfileId,
    decimal Amount,
    string CurrencyCode,
    string? GatewayPayoutId,
    DateTime RequestedAt
);
