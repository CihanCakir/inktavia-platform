using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.ApproveManualPayout;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.CancelInvoice;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.CancelParticipantSubscription;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.CancelPaymentTransaction;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.CancelProviderSubscription;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.CapturePayment;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.CreateInvoiceDraft;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.CreatePaymentEscrow;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.HoldPayout;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.IssueInvoice;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.MarkPayoutComplete;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.RefundPayment;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.ReinstatePayment;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.ReleasePaymentEscrow;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.ReversePartialRefund;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.SubscribeParticipant;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.SubscribeProvider;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetInvoiceDetail;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetInvoiceFull;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetInvoicesByBuyer;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetInvoicesPaged;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetParticipantPlans;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetParticipantSubscription;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPaymentDashboardKpis;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPaymentRefundHistory;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPaymentTransactionDetail;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPaymentTransactions;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPaymentTransactionStats;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPayoutDetail;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPayoutList;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPayoutStats;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPendingPayouts;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetProviderPlans;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetProviderSubscription;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetSubscriptionStats;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.CreateCommissionRule;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.DeactivateCommissionRule;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.ReactivateCommissionRule;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Command.UpdateCommissionRule;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetCommissionRuleDetail;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetCommissionRulesList;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetCommissionRuleStats;
using Aizen.Bff.AdminPanel.Application.AdminPayment.Query.ResolveCommissionRate;
using Aizen.Core.CQRS.Abstraction;
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
    private readonly IAizenCQRSProcessor _cqrs;

    public AdminPaymentController(
        IHttpContextAccessor  httpContextAccessor,
        IAizenCQRSProcessor   cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    // ─── Dashboard (gap report) ───────────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/dashboard/kpis</summary>
    [HttpGet("dashboard/kpis")]
    [ProducesResponseType(typeof(PaymentDashboardKpisBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PaymentDashboardKpisBffDto>> GetDashboardKpis(
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetPaymentDashboardKpisBffQuery(), ct);
        return SetResponse(result?.Kpis);
    }

    // ─── Transactions — read ──────────────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/transactions/stats</summary>
    [HttpGet("transactions/stats")]
    [ProducesResponseType(typeof(PaymentTransactionStatsBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PaymentTransactionStatsBffDto>> GetTransactionStats(
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetPaymentTransactionStatsBffQuery(), ct);
        return SetResponse(result?.Stats);
    }

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
        var result = await _cqrs.ProcessAsync(new GetPaymentTransactionsBffQuery
        {
            Status   = status,
            Type     = type,
            Gateway  = gateway,
            FromDate = fromDate,
            ToDate   = toDate,
            Search   = search,
            Page     = page,
            PageSize = pageSize,
        }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>GET api/v1/admin-panel/payment/transactions/{id}</summary>
    [HttpGet("transactions/{id:long}")]
    [ProducesResponseType(typeof(PaymentTransactionBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PaymentTransactionBffDto?>> GetTransaction(
        long id,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetPaymentTransactionDetailBffQuery { Id = id }, ct);
        return SetResponse(result?.Transaction);
    }

    /// <summary>GET api/v1/admin-panel/payment/admin/transactions/{id}/refund-history</summary>
    [HttpGet("admin/transactions/{id:long}/refund-history")]
    [ProducesResponseType(typeof(List<TransactionRefundRecordBffDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<TransactionRefundRecordBffDto>>> GetRefundHistory(
        long id,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetPaymentRefundHistoryBffQuery { TransactionId = id }, ct);
        return SetResponse(result?.Records);
    }

    // ─── Transactions — admin operations ─────────────────────────────────────

    /// <summary>POST api/v1/admin-panel/payment/admin/escrow — create escrow for SR offer acceptance</summary>
    [HttpPost("admin/escrow")]
    [ProducesResponseType(typeof(CreatePaymentEscrowResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CreatePaymentEscrowResult>> CreateEscrow(
        [FromBody] CreateEscrowRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new CreatePaymentEscrowBffCommand { Body = body }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>POST api/v1/admin-panel/payment/admin/transactions/{id}/capture</summary>
    [HttpPost("admin/transactions/{id:long}/capture")]
    [ProducesResponseType(typeof(CapturePaymentResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CapturePaymentResult>> Capture(
        long id,
        [FromBody] CapturePaymentRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new CapturePaymentBffCommand { Id = id, Body = body }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>POST api/v1/admin-panel/payment/admin/transactions/{id}/release</summary>
    [HttpPost("admin/transactions/{id:long}/release")]
    [ProducesResponseType(typeof(ReleasePaymentEscrowResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ReleasePaymentEscrowResult>> ReleaseEscrow(
        long id,
        [FromBody] ReleaseEscrowRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new ReleasePaymentEscrowBffCommand { Id = id, Body = body }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>POST api/v1/admin-panel/payment/admin/transactions/{id}/refund</summary>
    [HttpPost("admin/transactions/{id:long}/refund")]
    [ProducesResponseType(typeof(RefundPaymentResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RefundPaymentResult>> Refund(
        long id,
        [FromBody] RefundPaymentRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new RefundPaymentBffCommand { Id = id, Body = body }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>POST api/v1/admin-panel/payment/admin/transactions/{id}/cancel</summary>
    [HttpPost("admin/transactions/{id:long}/cancel")]
    [ProducesResponseType(typeof(CancelPaymentResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CancelPaymentResult>> CancelTransaction(
        long id,
        [FromBody] CancelPaymentRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new CancelPaymentTransactionBffCommand { Id = id, Body = body }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>POST api/v1/admin-panel/payment/admin/transactions/{id}/reinstate</summary>
    [HttpPost("admin/transactions/{id:long}/reinstate")]
    [ProducesResponseType(typeof(ReinstateCancelledTransactionResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ReinstateCancelledTransactionResult>> Reinstate(
        long id,
        [FromBody] ReinstatePaymentRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new ReinstatePaymentBffCommand { Id = id, Body = body }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>POST api/v1/admin-panel/payment/admin/refund-records/{refundRecordId}/reverse</summary>
    [HttpPost("admin/refund-records/{refundRecordId:long}/reverse")]
    [ProducesResponseType(typeof(ReversePartialRefundResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ReversePartialRefundResult>> ReverseRefund(
        long refundRecordId,
        [FromBody] ReverseRefundRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new ReversePartialRefundBffCommand { RefundRecordId = refundRecordId, Body = body }, ct);
        return SetResponse(result?.Result);
    }

    // ─── Payouts ──────────────────────────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/payouts/stats</summary>
    [HttpGet("payouts/stats")]
    [ProducesResponseType(typeof(PaymentPayoutStatsBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PaymentPayoutStatsBffDto>> GetPayoutStats(
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetPayoutStatsBffQuery(), ct);
        return SetResponse(result?.Stats);
    }

    /// <summary>GET api/v1/admin-panel/payment/payouts — full paged payout list with status filter</summary>
    [HttpGet("payouts")]
    [ProducesResponseType(typeof(PaymentPayoutListBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PaymentPayoutListBffResult>> GetPayouts(
        [FromQuery] string? status   = null,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetPayoutListBffQuery { Status = status, Page = page, PageSize = pageSize }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>GET api/v1/admin-panel/payment/payouts/{id}</summary>
    [HttpGet("payouts/{id:long}")]
    [ProducesResponseType(typeof(PaymentPayoutBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PaymentPayoutBffDto?>> GetPayoutDetail(
        long id,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetPayoutDetailBffQuery { Id = id }, ct);
        return SetResponse(result?.Payout);
    }

    /// <summary>GET api/v1/admin-panel/payment/payouts/pending</summary>
    [HttpGet("payouts/pending")]
    [ProducesResponseType(typeof(List<PendingPayoutBffDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<PendingPayoutBffDto>>> GetPendingPayouts(
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetPendingPayoutsBffQuery(), ct);
        return SetResponse(result?.Payouts);
    }

    /// <summary>POST api/v1/admin-panel/payment/payouts/{id}/complete</summary>
    [HttpPost("payouts/{id:long}/complete")]
    [ProducesResponseType(typeof(MarkPayoutCompleteResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MarkPayoutCompleteResult>> MarkPayoutComplete(
        long id,
        [FromBody] MarkPayoutCompleteRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new MarkPayoutCompleteBffCommand { Id = id, Body = body }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>POST api/v1/admin-panel/payment/payouts/{id}/hold</summary>
    [HttpPost("payouts/{id:long}/hold")]
    [ProducesResponseType(typeof(MarkPayoutCompleteResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MarkPayoutCompleteResult>> HoldPayout(
        long id,
        [FromBody] HoldPayoutRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new HoldPayoutBffCommand { Id = id, Body = body }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>POST api/v1/admin-panel/payment/payouts/{id}/approve-manual</summary>
    [HttpPost("payouts/{id:long}/approve-manual")]
    [ProducesResponseType(typeof(MarkPayoutCompleteResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MarkPayoutCompleteResult>> ApproveManualPayout(
        long id,
        [FromBody] ApproveManualPayoutRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new ApproveManualPayoutBffCommand { Id = id, Body = body }, ct);
        return SetResponse(result?.Result);
    }

    // ─── Subscriptions ────────────────────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/subscriptions/stats</summary>
    [HttpGet("subscriptions/stats")]
    [ProducesResponseType(typeof(SubscriptionStatsBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SubscriptionStatsBffDto>> GetSubscriptionStats(
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetSubscriptionStatsBffQuery(), ct);
        return SetResponse(result?.Stats);
    }

    /// <summary>POST api/v1/admin-panel/payment/subscriptions/provider</summary>
    [HttpPost("subscriptions/provider")]
    [ProducesResponseType(typeof(SubscribeProviderPlanResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SubscribeProviderPlanResult>> SubscribeProvider(
        [FromBody] SubscribeProviderPlanRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new SubscribeProviderBffCommand { Body = body }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>GET api/v1/admin-panel/payment/subscriptions/provider/{providerProfileId}</summary>
    [HttpGet("subscriptions/provider/{providerProfileId:long}")]
    [ProducesResponseType(typeof(ActiveProviderSubscriptionResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ActiveProviderSubscriptionResult?>> GetProviderSubscription(
        long providerProfileId,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetProviderSubscriptionBffQuery { ProviderProfileId = providerProfileId }, ct);
        return SetResponse(result?.Subscription);
    }

    /// <summary>DELETE api/v1/admin-panel/payment/subscriptions/provider/{providerProfileId}</summary>
    [HttpDelete("subscriptions/provider/{providerProfileId:long}")]
    [ProducesResponseType(typeof(CancelSubscriptionResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CancelSubscriptionResult>> CancelProviderSubscription(
        long providerProfileId,
        [FromQuery] string? reason = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new CancelProviderSubscriptionBffCommand { ProviderProfileId = providerProfileId, Reason = reason }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>POST api/v1/admin-panel/payment/subscriptions/participant</summary>
    [HttpPost("subscriptions/participant")]
    [ProducesResponseType(typeof(SubscribeParticipantPlanResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SubscribeParticipantPlanResult>> SubscribeParticipant(
        [FromBody] SubscribeParticipantPlanRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new SubscribeParticipantBffCommand { Body = body }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>GET api/v1/admin-panel/payment/subscriptions/participant/{participantProfileId}</summary>
    [HttpGet("subscriptions/participant/{participantProfileId:long}")]
    [ProducesResponseType(typeof(ActiveParticipantSubscriptionResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ActiveParticipantSubscriptionResult?>> GetParticipantSubscription(
        long participantProfileId,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetParticipantSubscriptionBffQuery { ParticipantProfileId = participantProfileId }, ct);
        return SetResponse(result?.Subscription);
    }

    /// <summary>DELETE api/v1/admin-panel/payment/subscriptions/participant/{participantProfileId}</summary>
    [HttpDelete("subscriptions/participant/{participantProfileId:long}")]
    [ProducesResponseType(typeof(CancelSubscriptionResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CancelSubscriptionResult>> CancelParticipantSubscription(
        long participantProfileId,
        [FromQuery] string? reason = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new CancelParticipantSubscriptionBffCommand { ParticipantProfileId = participantProfileId, Reason = reason }, ct);
        return SetResponse(result?.Result);
    }

    // ─── Plans ────────────────────────────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/provider-plans</summary>
    [HttpGet("provider-plans")]
    [ProducesResponseType(typeof(List<ProviderPlanBffDto>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<AizenApiResponse<List<ProviderPlanBffDto>>> GetProviderPlans(
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetProviderPlansBffQuery(), ct);
        return SetResponse(result?.Plans);
    }

    /// <summary>GET api/v1/admin-panel/payment/participant-plans</summary>
    [HttpGet("participant-plans")]
    [ProducesResponseType(typeof(List<ParticipantPlanBffDto>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<AizenApiResponse<List<ParticipantPlanBffDto>>> GetParticipantPlans(
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetParticipantPlansBffQuery(), ct);
        return SetResponse(result?.Plans);
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
        var result = await _cqrs.ProcessAsync(new ResolveCommissionRateBffQuery
        {
            ProviderProfileId = providerProfileId,
            ProviderPlanId    = providerPlanId,
            CategoryCode      = categoryCode,
        }, ct);
        return SetResponse(result?.Rate);
    }

    // ─── Commission Rules — CRUD ──────────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/commission/rules/stats — KPI strip</summary>
    [HttpGet("commission/rules/stats")]
    [ProducesResponseType(typeof(CommissionRuleStatsBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CommissionRuleStatsBffDto?>> GetCommissionRuleStats(
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetCommissionRuleStatsBffQuery(), ct);
        return SetResponse(result?.Stats);
    }

    /// <summary>GET api/v1/admin-panel/payment/commission/rules — paged list with filters</summary>
    [HttpGet("commission/rules")]
    [ProducesResponseType(typeof(CommissionRuleListBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CommissionRuleListBffResult>> GetCommissionRules(
        [FromQuery] string? ruleType  = null,
        [FromQuery] string? status    = null,
        [FromQuery] string? priority  = null,
        [FromQuery] int     page      = 1,
        [FromQuery] int     pageSize  = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetCommissionRulesListBffQuery
        {
            RuleType = ruleType,
            Status   = status,
            Priority = priority,
            Page     = page,
            PageSize = pageSize,
        }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>GET api/v1/admin-panel/payment/commission/rules/{id}</summary>
    [HttpGet("commission/rules/{id:long}")]
    [ProducesResponseType(typeof(CommissionRuleBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CommissionRuleBffDto?>> GetCommissionRuleById(
        long id,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetCommissionRuleDetailBffQuery { Id = id }, ct);
        return SetResponse(result?.Rule);
    }

    /// <summary>POST api/v1/admin-panel/payment/commission/rules — create new rule</summary>
    [HttpPost("commission/rules")]
    [ProducesResponseType(typeof(CommissionRuleCreateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CommissionRuleCreateBffResult>> CreateCommissionRule(
        [FromBody] CreateCommissionRuleBffRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new CreateCommissionRuleBffCommand { Body = body }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>PUT api/v1/admin-panel/payment/commission/rules/{id} — update mutable fields</summary>
    [HttpPut("commission/rules/{id:long}")]
    [ProducesResponseType(typeof(CommissionRuleMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CommissionRuleMutateBffResult>> UpdateCommissionRule(
        long id,
        [FromBody] UpdateCommissionRuleBffRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new UpdateCommissionRuleBffCommand { Id = id, Body = body }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>DELETE api/v1/admin-panel/payment/commission/rules/{id} — soft deactivate</summary>
    [HttpDelete("commission/rules/{id:long}")]
    [ProducesResponseType(typeof(CommissionRuleMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CommissionRuleMutateBffResult>> DeactivateCommissionRule(
        long id,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new DeactivateCommissionRuleBffCommand { Id = id }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>POST api/v1/admin-panel/payment/commission/rules/{id}/reactivate — re-activate an Inactive rule</summary>
    [HttpPost("commission/rules/{id:long}/reactivate")]
    [ProducesResponseType(typeof(CommissionRuleMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CommissionRuleMutateBffResult>> ReactivateCommissionRule(
        long id,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new ReactivateCommissionRuleBffCommand { Id = id }, ct);
        return SetResponse(result?.Result);
    }

    // ─── Invoices ─────────────────────────────────────────────────────────────

    /// <summary>POST api/v1/admin-panel/payment/invoices — create draft invoice</summary>
    [HttpPost("invoices")]
    [ProducesResponseType(typeof(CreateInvoiceDraftResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CreateInvoiceDraftResult>> CreateInvoiceDraft(
        [FromBody] CreateInvoiceDraftRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new CreateInvoiceDraftBffCommand { Body = body }, ct);
        return SetResponse(result?.Result);
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
        var result = await _cqrs.ProcessAsync(
            new GetInvoicesPagedBffQuery { Page = page, PageSize = pageSize, Status = status }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>GET api/v1/admin-panel/payment/invoices/{id} — header-only</summary>
    [HttpGet("invoices/{id:long}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<InvoiceDto?>> GetInvoice(
        long id,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetInvoiceDetailBffQuery { Id = id }, ct);
        return SetResponse(result?.Invoice);
    }

    /// <summary>GET api/v1/admin-panel/payment/invoices/{id}/full — with line items + tax breakdowns</summary>
    [HttpGet("invoices/{id:long}/full")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<InvoiceDto?>> GetInvoiceFull(
        long id,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetInvoiceFullBffQuery { Id = id }, ct);
        return SetResponse(result?.Invoice);
    }

    /// <summary>POST api/v1/admin-panel/payment/invoices/{id}/issue</summary>
    [HttpPost("invoices/{id:long}/issue")]
    [ProducesResponseType(typeof(IssueInvoiceResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IssueInvoiceResult>> IssueInvoice(
        long id,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new IssueInvoiceBffCommand { Id = id }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>DELETE api/v1/admin-panel/payment/invoices/{id} — cancel draft</summary>
    [HttpDelete("invoices/{id:long}")]
    [ProducesResponseType(typeof(CancelInvoiceResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CancelInvoiceResult>> CancelInvoice(
        long id,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new CancelInvoiceBffCommand { Id = id }, ct);
        return SetResponse(result?.Result);
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
        var result = await _cqrs.ProcessAsync(
            new GetInvoicesByBuyerBffQuery { BuyerId = buyerId, Page = page, PageSize = pageSize }, ct);
        return SetResponse(result?.Result);
    }
}
