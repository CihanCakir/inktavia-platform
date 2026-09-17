using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

namespace Aizen.Modules.Payment.Abstraction.RemoteCall;

/// <summary>
/// Server-to-server (internal) remote-call contract used by the <b>CargoDry</b> module to reach the Payment module when
/// the two run in separate pods (split-host dev topology: aizen-cargodry has no Payment.Application DI, so the in-process
/// <c>ICargoDrySettlementPayoutService</c> bridge cannot be activated → 911). This is the HTTP variant of that seam.
///
/// Auth model: the target endpoint is cluster-internal <c>[AllowAnonymous]</c> — the CargoDry host attaches no outbound
/// token, matching the existing token-less S2S reads (SR→ReferenceData, SR→Messaging). This is required because the
/// caller may be a background Hangfire job (the monthly settlement automation) with no user JWT to forward. The prepared
/// actor is carried in the request body (<c>PreparedByUserId</c>), and the underlying command is idempotent by
/// settlement id, so replay is safe. Internal endpoints are never exposed on the public API gateway.
///
/// BaseUrl is configured via <c>RemoteCalls__ICargoDrySettlementPaymentRemoteCall__BaseUrl</c> in the aizen-cargodry host.
/// </summary>
[DocumentationInfo("CargoDry → Payment internal remote call",
    "Lets the CargoDry module prepare a settlement payout record on the Payment module across a pod boundary.")]
public interface ICargoDrySettlementPaymentRemoteCall : IAizenRemoteCall
{
    /// <summary>
    /// Prepares (or returns the existing) PayoutRecord for a CargoDry sell-through settlement. Idempotent by
    /// SourceSettlementId. Does NOT execute any Iyzico transfer.
    /// </summary>
    [AizenRemoteCallPost("/api/v1/payment/internal/cargodry/settlement-payout/prepare")]
    Task<PrepareCargoDrySettlementPayoutRemoteCallResponse> PrepareSettlementPayoutAsync(
        [AizenRemoteCallBody] PrepareCargoDrySettlementPayoutRemoteCallRequest request,
        CancellationToken cancellationToken = default);

    // ── Payout lifecycle (Phase 4D) ────────────────────────────────────────────────
    // Admin-driven transitions on the PayoutRecord. None of these advances the settlement to Settled — that stays in the
    // CargoDry CompleteCargoDrySettlementPayout handler, which calls Complete below then MarkPayoutCompleted itself.

    /// <summary>Reads current payout state; validates the payout belongs to the given settlement.</summary>
    [AizenRemoteCallPost("/api/v1/payment/internal/cargodry/settlement-payout/{payoutRecordId}/state")]
    Task<CargoDryPayoutLifecycleResultDto> GetPayoutStateAsync(
        long payoutRecordId,
        [AizenRemoteCallBody] GetCargoDrySettlementPayoutStateRemoteCallRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Approves a Pending payout (Pending → Approved).</summary>
    [AizenRemoteCallPost("/api/v1/payment/internal/cargodry/settlement-payout/{payoutRecordId}/approve")]
    Task<CargoDryPayoutLifecycleResultDto> ApprovePayoutAsync(
        long payoutRecordId,
        [AizenRemoteCallBody] ApproveCargoDrySettlementPayoutRemoteCallRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Marks a payout as actively processing (Approved/Pending → Processing).</summary>
    [AizenRemoteCallPost("/api/v1/payment/internal/cargodry/settlement-payout/{payoutRecordId}/processing")]
    Task<CargoDryPayoutLifecycleResultDto> MarkPayoutProcessingAsync(
        long payoutRecordId,
        [AizenRemoteCallBody] MarkProcessingCargoDrySettlementPayoutRemoteCallRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Confirms a manual disbursement (→ Completed). Idempotent — a re-complete returns AlreadyCompleted.</summary>
    [AizenRemoteCallPost("/api/v1/payment/internal/cargodry/settlement-payout/{payoutRecordId}/complete")]
    Task<CargoDryPayoutLifecycleResultDto> CompletePayoutManualAsync(
        long payoutRecordId,
        [AizenRemoteCallBody] CompleteCargoDrySettlementPayoutRemoteCallRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Records a payout failure (→ Failed).</summary>
    [AizenRemoteCallPost("/api/v1/payment/internal/cargodry/settlement-payout/{payoutRecordId}/fail")]
    Task<CargoDryPayoutLifecycleResultDto> FailPayoutAsync(
        long payoutRecordId,
        [AizenRemoteCallBody] FailCargoDrySettlementPayoutRemoteCallRequest request,
        CancellationToken cancellationToken = default);

    // ── Invoice / statement (Phase 4C) + renewal invoice (Phase 11) ────────────────

    /// <summary>Prepares (or returns the existing) Draft ProviderSettlementStatement. Idempotent by settlement.</summary>
    [AizenRemoteCallPost("/api/v1/payment/internal/cargodry/settlement-statement/prepare")]
    Task<CreateCargoDrySettlementStatementResult> PrepareSettlementStatementAsync(
        [AizenRemoteCallBody] PrepareCargoDrySettlementStatementRemoteCallRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a Draft CargoDryInvoice for a kit renewal preparation. Does NOT create a PaymentTransaction.</summary>
    [AizenRemoteCallPost("/api/v1/payment/internal/cargodry/renewal-invoice/prepare")]
    Task<PrepareCargoDryRenewalInvoiceRemoteCallResponse> PrepareRenewalInvoiceAsync(
        [AizenRemoteCallBody] PrepareCargoDryRenewalInvoiceRemoteCallRequest request,
        CancellationToken cancellationToken = default);
}
