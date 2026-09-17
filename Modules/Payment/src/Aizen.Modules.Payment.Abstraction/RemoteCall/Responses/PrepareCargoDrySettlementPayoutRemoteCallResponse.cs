using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

/// <summary>
/// Split-host bridge response: the prepared (or pre-existing) PayoutRecord id + status, with an idempotency flag so the
/// CargoDry caller can distinguish a freshly created record from one that already existed.
/// </summary>
public sealed class PrepareCargoDrySettlementPayoutRemoteCallResponse
{
    public required long         PayoutRecordId { get; init; }
    public required PayoutStatus Status         { get; init; }
    public required bool         AlreadyExisted { get; init; }
}
