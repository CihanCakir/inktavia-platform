using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Model.Result;

/// <summary>
/// Result returned by CreateCargoDrySettlementStatementCommand.
/// Includes idempotency flag so the caller can distinguish between
/// a newly created draft statement and one that already existed.
/// Phase 4C (July 2026).
/// </summary>
public sealed record CreateCargoDrySettlementStatementResult(
    long          InvoiceId,
    string?       InvoiceNumber,   // null while Draft; assigned only at Issue
    InvoiceStatus InvoiceStatus,
    bool          AlreadyExisted
);
