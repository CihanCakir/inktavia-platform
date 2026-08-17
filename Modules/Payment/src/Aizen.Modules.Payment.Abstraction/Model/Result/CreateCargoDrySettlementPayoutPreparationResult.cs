using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Model.Result;

/// <summary>
/// Result returned by CreateCargoDrySettlementPayoutPreparationCommand.
/// Includes idempotency flag so the caller can distinguish between
/// a newly created record and one that already existed.
/// Phase 4B (July 2026).
/// </summary>
public sealed record CreateCargoDrySettlementPayoutPreparationResult(
    long         PayoutRecordId,
    PayoutStatus Status,
    bool         AlreadyExisted
);
