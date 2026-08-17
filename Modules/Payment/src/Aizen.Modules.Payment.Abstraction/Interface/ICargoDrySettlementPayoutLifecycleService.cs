using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Abstraction.Interface;

/// <summary>
/// Service contract allowing the CargoDry module to drive the payout record lifecycle
/// for CargoDry sell-through settlement payouts without taking a compile-time dependency
/// on Payment.Application.
///
/// Implemented in Payment.Application and registered in the shared DI container.
/// CargoDry.Application references Payment.Abstraction and injects this interface.
///
/// IMPORTANT: None of these methods call Iyzico, a bank API, or any real payment gateway.
/// All operations record payout lifecycle state only (audit trail).
/// The PayoutRecord is the Payment module source of truth for payout state.
///
/// Phase 4D (July 2026).
/// </summary>
public interface ICargoDrySettlementPayoutLifecycleService
{
    /// <summary>
    /// Returns the current state of a payout record for a given settlement.
    /// Validates that the payout belongs to the specified source settlement (idempotency guard).
    /// </summary>
    Task<CargoDryPayoutLifecycleResultDto> GetPayoutStateAsync(
        long              payoutRecordId,
        long              sourceSettlementId,
        CancellationToken ct = default);

    /// <summary>
    /// Admin approves a Pending payout record for disbursement.
    /// Transition: Pending → Approved.
    /// Does NOT call any payment gateway.
    /// </summary>
    Task<CargoDryPayoutLifecycleResultDto> ApproveAsync(
        long              payoutRecordId,
        long              approvedByUserId,
        string?           note,
        CancellationToken ct = default);

    /// <summary>
    /// Marks an Approved (or Pending) payout as actively in Processing.
    /// Records the admin user and an optional external reference (e.g. bank instruction ID).
    /// Transition: Approved/Pending → Processing.
    /// Does NOT call any payment gateway.
    /// </summary>
    Task<CargoDryPayoutLifecycleResultDto> MarkProcessingAsync(
        long              payoutRecordId,
        long              processedByUserId,
        string?           externalReference,
        string?           note,
        CancellationToken ct = default);

    /// <summary>
    /// Confirms that a manual disbursement has been executed.
    /// Idempotent — if the payout is already Completed, returns the existing state (AlreadyCompleted = true).
    /// Transition: Processing/Approved/Pending → Completed.
    /// Does NOT call any payment gateway.
    /// </summary>
    Task<CargoDryPayoutLifecycleResultDto> CompleteManualAsync(
        long              payoutRecordId,
        long              completedByUserId,
        string            manualPaymentReference,
        string?           note,
        CancellationToken ct = default);

    /// <summary>
    /// Records a payout failure with reason and responsible admin.
    /// Transition: any non-Completed, non-Cancelled state → Failed.
    /// Does NOT call any payment gateway.
    /// </summary>
    Task<CargoDryPayoutLifecycleResultDto> FailAsync(
        long              payoutRecordId,
        long              failedByUserId,
        string            failureReason,
        string?           externalReference,
        string?           note,
        CancellationToken ct = default);
}
