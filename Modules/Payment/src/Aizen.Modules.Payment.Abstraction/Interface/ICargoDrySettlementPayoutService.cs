using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Abstraction.Interface;

/// <summary>
/// Service contract allowing the CargoDry module to prepare payout records
/// for monthly sell-through settlements without taking a compile-time dependency
/// on Payment.Application.
///
/// Implemented in Payment.Application and registered in the shared DI container.
/// CargoDry.Application references Payment.Abstraction and injects this interface.
///
/// Phase 4B (July 2026).
/// </summary>
public interface ICargoDrySettlementPayoutService
{
    /// <summary>
    /// Prepares a PayoutRecord for a CargoDry sell-through settlement.
    /// Idempotent — if a payout record already exists for the given settlementId,
    /// returns the existing record (AlreadyExisted = true).
    ///
    /// Does NOT execute any Iyzico payout transfer.
    /// </summary>
    Task<CreateCargoDrySettlementPayoutPreparationResult> PrepareSettlementPayoutAsync(
        long              settlementId,
        string            settlementCode,
        long              providerProfileId,
        decimal           amount,
        string            currencyCode,
        string            description,
        long              preparedByUserId,
        CancellationToken ct = default);
}
