using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Request;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel/payment")]
[Tags("Admin Panel - Payment")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class AdminPaymentController : AizenWebApiController
{
    private readonly IAdminPaymentBffRemoteCall _payment;

    public AdminPaymentController(
        IHttpContextAccessor      httpContextAccessor,
        IAdminPaymentBffRemoteCall payment)
        : base(httpContextAccessor)
    {
        _payment = payment;
    }

    // ─── Transactions — read ──────────────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/transactions — paged list with optional filters</summary>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(PaymentTransactionListBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PaymentTransactionListBffResult>> GetTransactions(
        [FromQuery] PaymentTransactionStatus? status   = null,
        [FromQuery] TransactionType?          type     = null,
        [FromQuery] string?                   gateway  = null,
        [FromQuery] DateTime?                 fromDate = null,
        [FromQuery] DateTime?                 toDate   = null,
        [FromQuery] string?                   search   = null,
        [FromQuery] int                       page     = 1,
        [FromQuery] int                       pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _payment.GetTransactionsAsync(
            status, type, gateway, fromDate, toDate, search, page, pageSize, ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/payment/transactions/{id}</summary>
    [HttpGet("transactions/{id:long}")]
    [ProducesResponseType(typeof(PaymentTransactionBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PaymentTransactionBffDto?>> GetTransaction(
        long id,
        CancellationToken ct = default)
    {
        var result = await _payment.GetTransactionAsync(id, ct);
        return SetResponse(result);
    }

    // ─── Transactions — admin operations ─────────────────────────────────────

    /// <summary>POST api/v1/admin-panel/payment/admin/escrow — create escrow for SR offer acceptance</summary>
    [HttpPost("admin/escrow")]
    [ProducesResponseType(typeof(CreatePaymentEscrowResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CreatePaymentEscrowResult>> CreateEscrow(
        [FromBody] CreateEscrowRequest body,
        CancellationToken ct = default)
    {
        var result = await _payment.CreateEscrowAsync(body, ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/payment/admin/transactions/{id}/capture</summary>
    [HttpPost("admin/transactions/{id:long}/capture")]
    [ProducesResponseType(typeof(CapturePaymentResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CapturePaymentResult>> Capture(
        long id,
        [FromBody] CapturePaymentRequest body,
        CancellationToken ct = default)
    {
        var result = await _payment.CaptureAsync(id, body, ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/payment/admin/transactions/{id}/release</summary>
    [HttpPost("admin/transactions/{id:long}/release")]
    [ProducesResponseType(typeof(ReleasePaymentEscrowResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ReleasePaymentEscrowResult>> ReleaseEscrow(
        long id,
        [FromBody] ReleaseEscrowRequest body,
        CancellationToken ct = default)
    {
        var result = await _payment.ReleaseEscrowAsync(id, body, ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/payment/admin/transactions/{id}/refund</summary>
    [HttpPost("admin/transactions/{id:long}/refund")]
    [ProducesResponseType(typeof(RefundPaymentResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RefundPaymentResult>> Refund(
        long id,
        [FromBody] RefundPaymentRequest body,
        CancellationToken ct = default)
    {
        var result = await _payment.RefundAsync(id, body, ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/payment/admin/transactions/{id}/cancel</summary>
    [HttpPost("admin/transactions/{id:long}/cancel")]
    [ProducesResponseType(typeof(CancelPaymentResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CancelPaymentResult>> CancelTransaction(
        long id,
        [FromBody] CancelPaymentRequest body,
        CancellationToken ct = default)
    {
        var result = await _payment.CancelTransactionAsync(id, body, ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/payment/admin/transactions/{id}/reinstate</summary>
    [HttpPost("admin/transactions/{id:long}/reinstate")]
    [ProducesResponseType(typeof(ReinstateCancelledTransactionResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ReinstateCancelledTransactionResult>> Reinstate(
        long id,
        [FromBody] ReinstatePaymentRequest body,
        CancellationToken ct = default)
    {
        var result = await _payment.ReinstateAsync(id, body, ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/payment/admin/refund-records/{refundRecordId}/reverse</summary>
    [HttpPost("admin/refund-records/{refundRecordId:long}/reverse")]
    [ProducesResponseType(typeof(ReversePartialRefundResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ReversePartialRefundResult>> ReverseRefund(
        long refundRecordId,
        [FromBody] ReverseRefundRequest body,
        CancellationToken ct = default)
    {
        var result = await _payment.ReverseRefundAsync(refundRecordId, body, ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/payment/admin/transactions/{id}/refund-history</summary>
    [HttpGet("admin/transactions/{id:long}/refund-history")]
    [ProducesResponseType(typeof(List<TransactionRefundRecordBffDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<TransactionRefundRecordBffDto>>> GetRefundHistory(
        long id,
        CancellationToken ct = default)
    {
        var result = await _payment.GetRefundHistoryAsync(id, ct);
        return SetResponse(result);
    }

    // ─── Payouts ──────────────────────────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/payouts/pending</summary>
    [HttpGet("payouts/pending")]
    [ProducesResponseType(typeof(List<PendingPayoutBffDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<PendingPayoutBffDto>>> GetPendingPayouts(
        CancellationToken ct = default)
    {
        var result = await _payment.GetPendingPayoutsAsync(ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/payment/payouts/{id}/complete</summary>
    [HttpPost("payouts/{id:long}/complete")]
    [ProducesResponseType(typeof(MarkPayoutCompleteResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MarkPayoutCompleteResult>> MarkPayoutComplete(
        long id,
        [FromBody] MarkPayoutCompleteRequest body,
        CancellationToken ct = default)
    {
        var result = await _payment.MarkPayoutCompleteAsync(id, body, ct);
        return SetResponse(result);
    }

    // ─── Subscriptions ────────────────────────────────────────────────────────

    /// <summary>POST api/v1/admin-panel/payment/subscriptions/provider</summary>
    [HttpPost("subscriptions/provider")]
    [ProducesResponseType(typeof(SubscribeProviderPlanResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SubscribeProviderPlanResult>> SubscribeProvider(
        [FromBody] SubscribeProviderPlanRequest body,
        CancellationToken ct = default)
    {
        var result = await _payment.SubscribeProviderAsync(body, ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/payment/subscriptions/provider/{providerProfileId}</summary>
    [HttpGet("subscriptions/provider/{providerProfileId:long}")]
    [ProducesResponseType(typeof(ActiveProviderSubscriptionResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ActiveProviderSubscriptionResult?>> GetProviderSubscription(
        long providerProfileId,
        CancellationToken ct = default)
    {
        var result = await _payment.GetProviderSubscriptionAsync(providerProfileId, ct);
        return SetResponse(result);
    }

    /// <summary>DELETE api/v1/admin-panel/payment/subscriptions/provider/{providerProfileId}</summary>
    [HttpDelete("subscriptions/provider/{providerProfileId:long}")]
    [ProducesResponseType(typeof(CancelSubscriptionResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CancelSubscriptionResult>> CancelProviderSubscription(
        long providerProfileId,
        [FromQuery] string? reason = null,
        CancellationToken ct = default)
    {
        var result = await _payment.CancelProviderSubscriptionAsync(providerProfileId, reason, ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/payment/subscriptions/participant</summary>
    [HttpPost("subscriptions/participant")]
    [ProducesResponseType(typeof(SubscribeParticipantPlanResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SubscribeParticipantPlanResult>> SubscribeParticipant(
        [FromBody] SubscribeParticipantPlanRequest body,
        CancellationToken ct = default)
    {
        var result = await _payment.SubscribeParticipantAsync(body, ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/payment/subscriptions/participant/{participantProfileId}</summary>
    [HttpGet("subscriptions/participant/{participantProfileId:long}")]
    [ProducesResponseType(typeof(ActiveParticipantSubscriptionResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ActiveParticipantSubscriptionResult?>> GetParticipantSubscription(
        long participantProfileId,
        CancellationToken ct = default)
    {
        var result = await _payment.GetParticipantSubscriptionAsync(participantProfileId, ct);
        return SetResponse(result);
    }

    /// <summary>DELETE api/v1/admin-panel/payment/subscriptions/participant/{participantProfileId}</summary>
    [HttpDelete("subscriptions/participant/{participantProfileId:long}")]
    [ProducesResponseType(typeof(CancelSubscriptionResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CancelSubscriptionResult>> CancelParticipantSubscription(
        long participantProfileId,
        [FromQuery] string? reason = null,
        CancellationToken ct = default)
    {
        var result = await _payment.CancelParticipantSubscriptionAsync(participantProfileId, reason, ct);
        return SetResponse(result);
    }

    // ─── Plans ────────────────────────────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/provider-plans</summary>
    [HttpGet("provider-plans")]
    [ProducesResponseType(typeof(List<ProviderPlanBffDto>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<AizenApiResponse<List<ProviderPlanBffDto>>> GetProviderPlans(
        CancellationToken ct = default)
    {
        var result = await _payment.GetProviderPlansAsync(ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/payment/participant-plans</summary>
    [HttpGet("participant-plans")]
    [ProducesResponseType(typeof(List<ParticipantPlanBffDto>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<AizenApiResponse<List<ParticipantPlanBffDto>>> GetParticipantPlans(
        CancellationToken ct = default)
    {
        var result = await _payment.GetParticipantPlansAsync(ct);
        return SetResponse(result);
    }

    // ─── Commission ───────────────────────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/commission/resolve</summary>
    [HttpGet("commission/resolve")]
    [ProducesResponseType(typeof(CommissionRateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CommissionRateBffResult>> ResolveCommissionRate(
        [FromQuery] long?   providerProfileId = null,
        [FromQuery] long?   providerPlanId    = null,
        [FromQuery] string? categoryCode      = null,
        CancellationToken ct = default)
    {
        var result = await _payment.ResolveCommissionRateAsync(
            providerProfileId, providerPlanId, categoryCode, ct);
        return SetResponse(result);
    }

    // ─── Invoices ─────────────────────────────────────────────────────────────

    /// <summary>POST api/v1/admin-panel/payment/invoices — create draft invoice</summary>
    [HttpPost("invoices")]
    [ProducesResponseType(typeof(CreateInvoiceDraftResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CreateInvoiceDraftResult>> CreateInvoiceDraft(
        [FromBody] CreateInvoiceDraftRequest body,
        CancellationToken ct = default)
    {
        var result = await _payment.CreateInvoiceDraftAsync(body, ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/payment/invoices — paged invoice list</summary>
    [HttpGet("invoices")]
    [ProducesResponseType(typeof(InvoiceListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<InvoiceListResult>> GetInvoicesPaged(
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 25,
        [FromQuery] string? status   = null,
        CancellationToken ct = default)
    {
        var result = await _payment.GetInvoicesPagedAsync(page, pageSize, status, ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/payment/invoices/{id} — header-only</summary>
    [HttpGet("invoices/{id:long}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<InvoiceDto?>> GetInvoice(
        long id,
        CancellationToken ct = default)
    {
        var result = await _payment.GetInvoiceAsync(id, ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/payment/invoices/{id}/full — with line items + tax breakdowns</summary>
    [HttpGet("invoices/{id:long}/full")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<InvoiceDto?>> GetInvoiceFull(
        long id,
        CancellationToken ct = default)
    {
        var result = await _payment.GetInvoiceFullAsync(id, ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/payment/invoices/{id}/issue</summary>
    [HttpPost("invoices/{id:long}/issue")]
    [ProducesResponseType(typeof(IssueInvoiceResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IssueInvoiceResult>> IssueInvoice(
        long id,
        CancellationToken ct = default)
    {
        var result = await _payment.IssueInvoiceAsync(id, ct);
        return SetResponse(result);
    }

    /// <summary>DELETE api/v1/admin-panel/payment/invoices/{id} — cancel draft</summary>
    [HttpDelete("invoices/{id:long}")]
    [ProducesResponseType(typeof(CancelInvoiceResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CancelInvoiceResult>> CancelInvoice(
        long id,
        CancellationToken ct = default)
    {
        var result = await _payment.CancelInvoiceAsync(id, ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/payment/invoices/buyer/{buyerId}</summary>
    [HttpGet("invoices/buyer/{buyerId:long}")]
    [ProducesResponseType(typeof(InvoiceListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<InvoiceListResult>> GetInvoicesByBuyer(
        long buyerId,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _payment.GetInvoicesByBuyerAsync(buyerId, page, pageSize, ct);
        return SetResponse(result);
    }
}
