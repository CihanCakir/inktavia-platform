using Aizen.Modules.Payment.Abstraction.Interface;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using Aizen.Modules.Payment.Application.Commands.CreateCargoDrySettlementPayoutPreparation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

/// <summary>
/// Cluster-internal (service-to-service) endpoints called by the <b>CargoDry</b> module when it runs in a separate pod
/// from Payment. These are <see cref="AllowAnonymousAttribute">[AllowAnonymous]</see> — token-less — because the caller
/// can be a background Hangfire job (the monthly settlement automation) with no user JWT to forward, mirroring the
/// existing token-less S2S reads (SR→ReferenceData, SR→Messaging). Security is by network isolation: these routes are
/// never published on the public API gateway. The prepared actor travels in the request body; the underlying command is
/// idempotent by settlement id, so replay is safe.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/payment/internal/cargodry")]
public sealed class PaymentCargoDryInternalController : ControllerBase
{
    private readonly ISender                                   _sender;
    private readonly ICargoDrySettlementPayoutLifecycleService _lifecycle;
    private readonly ICargoDrySettlementInvoiceService         _invoice;
    private readonly ICargoDryRenewalInvoiceService            _renewalInvoice;

    public PaymentCargoDryInternalController(
        ISender                                   sender,
        ICargoDrySettlementPayoutLifecycleService lifecycle,
        ICargoDrySettlementInvoiceService         invoice,
        ICargoDryRenewalInvoiceService            renewalInvoice)
    {
        _sender         = sender;
        _lifecycle      = lifecycle;
        _invoice        = invoice;
        _renewalInvoice = renewalInvoice;
    }

    /// <summary>
    /// Prepares (or returns the existing) PayoutRecord for a CargoDry sell-through settlement. Idempotent by
    /// SourceSettlementId. Does NOT create a PaymentTransaction, Invoice, or execute any Iyzico transfer.
    /// </summary>
    [HttpPost("settlement-payout/prepare")]
    public async Task<IActionResult> PrepareSettlementPayout(
        [FromBody] PrepareCargoDrySettlementPayoutRemoteCallRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new CreateCargoDrySettlementPayoutPreparationCommand
        {
            SourceSettlementId = request.SourceSettlementId,
            SettlementCode     = request.SettlementCode,
            ProviderProfileId  = request.ProviderProfileId,
            Amount             = request.Amount,
            CurrencyCode       = request.CurrencyCode,
            Description        = request.Description,
            PreparedByUserId   = request.PreparedByUserId,
        }, ct);

        return Ok(new PrepareCargoDrySettlementPayoutRemoteCallResponse
        {
            PayoutRecordId = result.PayoutRecordId,
            Status         = result.Status,
            AlreadyExisted = result.AlreadyExisted,
        });
    }

    // ── Payout lifecycle (Phase 4D) — thin passthrough to the in-process bridge service ─────────────
    // These move the PayoutRecord only. Settlement closure (→ Settled) stays in CargoDry's
    // CompleteCargoDrySettlementPayout handler, which calls /complete then MarkPayoutCompleted itself.

    [HttpPost("settlement-payout/{payoutRecordId:long}/state")]
    public async Task<IActionResult> GetPayoutState(
        long payoutRecordId, [FromBody] GetCargoDrySettlementPayoutStateRemoteCallRequest request, CancellationToken ct)
        => Ok(await _lifecycle.GetPayoutStateAsync(payoutRecordId, request.SourceSettlementId, ct));

    [HttpPost("settlement-payout/{payoutRecordId:long}/approve")]
    public async Task<IActionResult> ApprovePayout(
        long payoutRecordId, [FromBody] ApproveCargoDrySettlementPayoutRemoteCallRequest request, CancellationToken ct)
        => Ok(await _lifecycle.ApproveAsync(payoutRecordId, request.ApprovedByUserId, request.Note, ct));

    [HttpPost("settlement-payout/{payoutRecordId:long}/processing")]
    public async Task<IActionResult> MarkPayoutProcessing(
        long payoutRecordId, [FromBody] MarkProcessingCargoDrySettlementPayoutRemoteCallRequest request, CancellationToken ct)
        => Ok(await _lifecycle.MarkProcessingAsync(
            payoutRecordId, request.ProcessedByUserId, request.ExternalReference, request.Note, ct));

    /// <summary>Confirms manual disbursement (payout → Completed). Idempotent. Does NOT advance the settlement itself.</summary>
    [HttpPost("settlement-payout/{payoutRecordId:long}/complete")]
    public async Task<IActionResult> CompletePayoutManual(
        long payoutRecordId, [FromBody] CompleteCargoDrySettlementPayoutRemoteCallRequest request, CancellationToken ct)
        => Ok(await _lifecycle.CompleteManualAsync(
            payoutRecordId, request.CompletedByUserId, request.ManualPaymentReference, request.Note, ct));

    [HttpPost("settlement-payout/{payoutRecordId:long}/fail")]
    public async Task<IActionResult> FailPayout(
        long payoutRecordId, [FromBody] FailCargoDrySettlementPayoutRemoteCallRequest request, CancellationToken ct)
        => Ok(await _lifecycle.FailAsync(
            payoutRecordId, request.FailedByUserId, request.FailureReason, request.ExternalReference, request.Note, ct));

    // ── Invoice / statement (Phase 4C) + renewal invoice (Phase 11) ────────────────────────────────

    [HttpPost("settlement-statement/prepare")]
    public async Task<IActionResult> PrepareSettlementStatement(
        [FromBody] PrepareCargoDrySettlementStatementRemoteCallRequest request, CancellationToken ct)
        => Ok(await _invoice.PrepareSettlementStatementAsync(
            request.SettlementId, request.SettlementCode, request.ProviderProfileId, request.ProviderPayoutAmount,
            request.TotalSaleAmount, request.TotalCommissionAmount, request.TotalKitCount, request.CurrencyCode,
            request.ProductCode, request.PeriodStartUtc, request.PeriodEndUtc, request.PayoutRecordId,
            request.PreparedByUserId, request.Notes, ct));

    [HttpPost("renewal-invoice/prepare")]
    public async Task<IActionResult> PrepareRenewalInvoice(
        [FromBody] PrepareCargoDryRenewalInvoiceRemoteCallRequest request, CancellationToken ct)
    {
        var invoiceId = await _renewalInvoice.PrepareRenewalInvoiceAsync(
            request.RenewalPreparationId, request.RenewalCode, request.KitId, request.KitCode, request.ProductCode,
            request.ProductName, request.OwnerUserId, request.RenewalPrice, request.CurrencyCode,
            request.RenewalMonths, request.Note, ct);

        return Ok(new PrepareCargoDryRenewalInvoiceRemoteCallResponse { InvoiceId = invoiceId });
    }
}
