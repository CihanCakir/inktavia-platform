using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Requests;
using Aizen.Modules.Payment.Application.Commands.CancelPayment;
using Aizen.Modules.Payment.Application.Commands.CapturePayment;
using Aizen.Modules.Payment.Application.Commands.CreatePaymentEscrow;
using Aizen.Modules.Payment.Application.Commands.RefundPayment;
using Aizen.Modules.Payment.Application.Commands.ReinstateCancelledTransaction;
using Aizen.Modules.Payment.Application.Commands.ReversePartialRefund;
using Aizen.Modules.Payment.Application.Queries.GetTransactionRefundHistory;
using Aizen.Modules.Payment.Application.Commands.ReleasePaymentEscrow;
using Aizen.Modules.Payment.Application.Queries.GetPaymentDashboardKpis;
using Aizen.Modules.Payment.Application.Queries.GetPaymentTransactionStats;
using Aizen.Modules.Payment.Application.Queries.GetSubscriptionStats;
using Aizen.Modules.Payment.Application.Queries.GetAdminSubscriptionList;
using Aizen.Modules.Payment.Application.Queries.GetSubscriptionMrrTrend;
using Aizen.Modules.Payment.Application.Queries.GetSubscriptionChurnRisk;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/payment/admin")]
public sealed class PaymentAdminController : ControllerBase
{
    private readonly ISender _sender;
    public PaymentAdminController(ISender sender) => _sender = sender;

    // ── Escrow lifecycle ──────────────────────────────────────────────────────

    /// <summary>Creates a new escrow transaction. Called from ServiceRequest module after offer acceptance.</summary>
    [HttpPost("escrow")]
    public async Task<IActionResult> CreateEscrow(
        [FromBody] CreateEscrowRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new CreatePaymentEscrowCommand
        {
            IdempotencyKey     = request.IdempotencyKey,
            Context            = request.Context,
            TransactionType    = request.TransactionType,
            PayerProfileId     = request.PayerProfileId,
            RecipientProfileId = request.RecipientProfileId,
            GrossAmount        = request.GrossAmount,
            DiscountAmount     = request.DiscountAmount,
            CurrencyCode       = request.CurrencyCode,
            ProviderPlanId     = request.ProviderPlanId,
            CategoryCode       = request.CategoryCode,
            EscrowRequired     = request.EscrowRequired,
        }, ct);
        return Ok(result);
    }

    /// <summary>Confirms payment capture (gateway webhook confirmation for manual gateway).</summary>
    [HttpPost("transactions/{id:long}/capture")]
    public async Task<IActionResult> Capture(
        long id, [FromBody] CapturePaymentRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new CapturePaymentCommand
        {
            TransactionId    = id,
            GatewayReference = request.GatewayReference,
            PaidAmount       = request.PaidAmount,
            CurrencyCode     = request.CurrencyCode,
        }, ct);
        return Ok(result);
    }

    /// <summary>Releases escrow after SR completion approval. Creates a PayoutRecord.</summary>
    [HttpPost("transactions/{id:long}/release")]
    public async Task<IActionResult> ReleaseEscrow(
        long id, [FromBody] ReleaseEscrowRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new ReleasePaymentEscrowCommand
        {
            TransactionId    = id,
            ApprovedByUserId = request.ApprovedByUserId,
            AdminNote        = request.AdminNote,
        }, ct);
        return Ok(result);
    }

    // ── Refund operations ─────────────────────────────────────────────────────

    /// <summary>
    /// Issues a full or partial gateway refund for a Captured/Released/PartiallyRefunded transaction.
    /// Creates a TransactionRefundRecord and updates TotalRefundedAmount on the transaction.
    /// </summary>
    [HttpPost("transactions/{id:long}/refund")]
    public async Task<IActionResult> Refund(
        long id, [FromBody] RefundPaymentRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new RefundPaymentCommand
        {
            TransactionId = id,
            RefundAmount  = request.RefundAmount,
            Reason        = request.Reason,
            RefundType    = request.RefundType,
            AdminNote     = request.AdminNote,
        }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Reverses a previously processed refund record (e.g., issued in error before bank settlement).
    /// The RefundRecord is marked Reversed — not deleted. TotalRefundedAmount is recalculated.
    /// </summary>
    [HttpPost("refund-records/{refundRecordId:long}/reverse")]
    public async Task<IActionResult> ReverseRefund(
        long refundRecordId, [FromBody] ReverseRefundRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new ReversePartialRefundCommand
        {
            RefundRecordId = refundRecordId,
            ReversalReason = request.ReversalReason,
            AdminNote      = request.AdminNote,
        }, ct);
        return Ok(result);
    }

    /// <summary>Returns the full refund history (all TransactionRefundRecords) for a transaction.</summary>
    [HttpGet("transactions/{id:long}/refund-history")]
    public async Task<IActionResult> GetRefundHistory(long id, CancellationToken ct)
    {
        var result = await _sender.Send(
            new GetTransactionRefundHistoryQuery { TransactionId = id }, ct);
        return Ok(result);
    }

    // ── Dashboard KPI stats ───────────────────────────────────────────────────

    /// <summary>Returns KPI strip for the payment admin dashboard (gross volume, commission, escrow, pending counts).</summary>
    [HttpGet("dashboard/kpis")]
    public async Task<IActionResult> GetDashboardKpis(CancellationToken ct)
    {
        var result = await _sender.Send(new GetPaymentDashboardKpisQuery(), ct);
        return Ok(result);
    }

    /// <summary>Returns transaction ledger KPI cards (NetLiquidity, PendingClearances, OperationalBurn, FleetRoi).</summary>
    [HttpGet("transactions/stats")]
    public async Task<IActionResult> GetTransactionStats(CancellationToken ct)
    {
        var result = await _sender.Send(new GetPaymentTransactionStatsQuery(), ct);
        return Ok(result);
    }

    /// <summary>Returns subscription KPI strip (active counts, MRR, failed renewals, expiring soon).</summary>
    [HttpGet("subscriptions/stats")]
    public async Task<IActionResult> GetSubscriptionStats(CancellationToken ct)
    {
        var result = await _sender.Send(new GetSubscriptionStatsQuery(), ct);
        return Ok(result);
    }

    /// <summary>GET admin/subscriptions — paged merged list of provider + participant subscriptions.</summary>
    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetAdminSubscriptionList(
        [FromQuery] string? audience = null,
        [FromQuery] string? status   = null,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetAdminSubscriptionListQuery
        {
            Audience = audience,
            Status   = status,
            Page     = page,
            PageSize = pageSize,
        }, ct);
        return Ok(result);
    }

    /// <summary>GET admin/subscriptions/mrr-trend — monthly MRR totals for the last N months.</summary>
    [HttpGet("subscriptions/mrr-trend")]
    public async Task<IActionResult> GetSubscriptionMrrTrend(
        [FromQuery] int months = 6,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetSubscriptionMrrTrendQuery { Months = months }, ct);
        return Ok(result);
    }

    /// <summary>GET admin/subscriptions/churn-risk — churn risk signal counts.</summary>
    [HttpGet("subscriptions/churn-risk")]
    public async Task<IActionResult> GetSubscriptionChurnRisk(CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetSubscriptionChurnRiskQuery(), ct);
        return Ok(result);
    }

    // ── Cancellation operations ───────────────────────────────────────────────

    /// <summary>
    /// Cancels a PendingIntent transaction before any money is captured.
    /// No gateway call needed — intent is voided in-system.
    /// </summary>
    [HttpPost("transactions/{id:long}/cancel")]
    public async Task<IActionResult> Cancel(
        long id, [FromBody] CancelPaymentRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new CancelPaymentCommand
        {
            TransactionId      = id,
            CancellationReason = request.CancellationReason,
            AdminNote          = request.AdminNote,
        }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Reinstates a previously cancelled transaction back to PendingIntent.
    /// Use when a cancellation was made in error and the payer needs to retry.
    /// </summary>
    [HttpPost("transactions/{id:long}/reinstate")]
    public async Task<IActionResult> Reinstate(
        long id, [FromBody] ReinstatePaymentRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new ReinstateCancelledTransactionCommand
        {
            TransactionId = id,
            AdminNote     = request.AdminNote,
        }, ct);
        return Ok(result);
    }

    // ── Provider sub-merchant onboarding review (BE-I1) ─────────────────────────

    /// <summary>Paged admin sub-merchant onboarding review queue (optional status filter).</summary>
    [HttpGet("providers/sub-merchant/onboarding-queue")]
    public async Task<IActionResult> GetSubMerchantOnboardingQueue(
        [FromQuery] ProviderSubMerchantOnboardingStatus? status = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _sender.Send(new Application.Queries.GetProviderSubMerchantOnboardingQueue
            .GetProviderSubMerchantOnboardingQueueQuery { Status = status, Page = page, PageSize = pageSize }, ct));

    /// <summary>Admin review complete: advances a provider's sub-merchant onboarding SubMerchantCreated → Verified.</summary>
    [HttpPost("providers/{providerProfileId:long}/sub-merchant/verify")]
    public async Task<IActionResult> VerifyProviderSubMerchant(long providerProfileId, CancellationToken ct)
        => Ok(await _sender.Send(new Application.Commands.MarkProviderSubMerchantVerified
            .MarkProviderSubMerchantVerifiedCommand { ProviderProfileId = providerProfileId }, ct));

    /// <summary>Admin rejects a provider's sub-merchant onboarding (→ Rejected, not split-eligible).</summary>
    [HttpPost("providers/{providerProfileId:long}/sub-merchant/reject")]
    public async Task<IActionResult> RejectProviderSubMerchant(
        long providerProfileId, [FromBody] RejectProviderSubMerchantRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new Application.Commands.RejectProviderSubMerchant
            .RejectProviderSubMerchantCommand { ProviderProfileId = providerProfileId, Reason = request?.Reason }, ct));

    /// <summary>Admin registers a provider's sub-merchant (#113): {DataSubmitted, Rejected} → SubMerchantCreated.
    /// Manual/dev gateway mints a synthetic key; iyzico uses the P9 registration. Idempotent.</summary>
    [HttpPost("providers/{providerProfileId:long}/sub-merchant/register")]
    public async Task<IActionResult> RegisterProviderSubMerchant(
        long providerProfileId, [FromBody] RegisterProviderSubMerchantRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new Application.Commands.RegisterProviderSubMerchant.RegisterProviderSubMerchantCommand
        {
            ProviderProfileId = providerProfileId,
            LegalName      = request?.LegalName,
            Email          = request?.Email,
            Iban           = request?.Iban,
            SubMerchantType = request?.SubMerchantType,
            TaxNumber      = request?.TaxNumber,
            TaxOffice      = request?.TaxOffice,
            GsmNumber      = request?.GsmNumber,
            ContactName    = request?.ContactName,
            ContactSurname = request?.ContactSurname,
            IdentityNumber = request?.IdentityNumber,
        }, ct));
}

/// <summary>Admin body for rejecting a provider sub-merchant onboarding (BE-I1).</summary>
public sealed class RejectProviderSubMerchantRequest
{
    public string? Reason { get; init; }
}

/// <summary>Admin body for registering a provider sub-merchant (BE-I1/#113). All fields optional — required only for
/// the iyzico gateway; the manual/dev gateway ignores them (mints a synthetic key from the existing profile).</summary>
public sealed class RegisterProviderSubMerchantRequest
{
    public string? LegalName      { get; init; }
    public string? Email          { get; init; }
    public string? Iban           { get; init; }
    public string? SubMerchantType { get; init; }
    public string? TaxNumber      { get; init; }
    public string? TaxOffice      { get; init; }
    public string? GsmNumber      { get; init; }
    public string? ContactName    { get; init; }
    public string? ContactSurname { get; init; }
    public string? IdentityNumber { get; init; }
}

