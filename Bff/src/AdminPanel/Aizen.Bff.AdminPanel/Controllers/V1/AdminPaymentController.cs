using Aizen.Bff.AdminPanel.Application.Payment.Command.ApproveManualPayout;
using Aizen.Bff.AdminPanel.Application.Payment.Command.CancelInvoice;
using Aizen.Bff.AdminPanel.Application.Payment.Command.CancelParticipantSubscription;
using Aizen.Bff.AdminPanel.Application.Payment.Command.CancelPaymentTransaction;
using Aizen.Bff.AdminPanel.Application.Payment.Command.CancelProviderSubscription;
using Aizen.Bff.AdminPanel.Application.Payment.Command.CapturePayment;
using Aizen.Bff.AdminPanel.Application.Payment.Command.CreateInvoiceDraft;
using Aizen.Bff.AdminPanel.Application.Payment.Command.CreatePaymentEscrow;
using Aizen.Bff.AdminPanel.Application.Payment.Command.HoldPayout;
using Aizen.Bff.AdminPanel.Application.Payment.Command.IssueInvoice;
using Aizen.Bff.AdminPanel.Application.Payment.Command.MarkPayoutComplete;
using Aizen.Bff.AdminPanel.Application.Payment.Command.RefundPayment;
using Aizen.Bff.AdminPanel.Application.Payment.Command.ReinstatePayment;
using Aizen.Bff.AdminPanel.Application.Payment.Command.ReleasePaymentEscrow;
using Aizen.Bff.AdminPanel.Application.Payment.Command.ReversePartialRefund;
using Aizen.Bff.AdminPanel.Application.Payment.Command.SubscribeParticipant;
using Aizen.Bff.AdminPanel.Application.Payment.Command.SubscribeProvider;
using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetInvoiceDetail;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetInvoiceFull;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetInvoicesByBuyer;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetInvoicesPaged;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetParticipantPlans;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetParticipantSubscription;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetPaymentDashboardKpis;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetPaymentRefundHistory;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetPaymentTransactionDetail;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetPaymentTransactions;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetPaymentTransactionStats;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetPayoutDetail;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetPayoutList;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetPayoutStats;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetPendingPayouts;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderPlans;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderSubscription;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetSubscriptionStats;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetAdminSubscriptionList;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetSubscriptionMrrTrend;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetSubscriptionChurnRisk;
using Aizen.Bff.AdminPanel.Application.Payment.Command.CreateCommissionRule;
using Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivateCommissionRule;
using Aizen.Bff.AdminPanel.Application.Payment.Command.ReactivateCommissionRule;
using Aizen.Bff.AdminPanel.Application.Payment.Command.RejectSubMerchant;
using Aizen.Bff.AdminPanel.Application.Payment.Command.UpdateCommissionRule;
using Aizen.Bff.AdminPanel.Application.Payment.Command.VerifySubMerchant;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetSubMerchantOnboardingQueue;
using Aizen.Bff.AdminPanel.Application.Payment.PlatformFeeRule;
using Aizen.Bff.AdminPanel.Application.Payment.ProviderPlanPrice;
using Aizen.Bff.AdminPanel.Application.Payment.ProfitProtectionPolicy;
using Aizen.Bff.AdminPanel.Application.Payment.CustomerDiscount;
using Aizen.Bff.AdminPanel.Application.Payment.ProviderCommissionBenefit;
using Aizen.Bff.AdminPanel.Application.Payment.PremiumAdmin;
using Aizen.Bff.AdminPanel.Application.Payment.RefundAllocationPolicy;
using Aizen.Bff.AdminPanel.Application.Payment.RefundQueue;
using Aizen.Bff.AdminPanel.Application.Payment.ProviderBalance;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetCommissionRuleDetail;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetCommissionRulesList;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetCommissionRuleStats;
using Aizen.Bff.AdminPanel.Application.Payment.Query.ResolveCommissionRate;
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
using Aizen.Bff.AdminPanel.Application.Payment.Command.ActivateParticipantPlan;
using Aizen.Bff.AdminPanel.Application.Payment.Command.ActivateProviderPlan;
using Aizen.Bff.AdminPanel.Application.Payment.Command.CreateParticipantPlan;
using Aizen.Bff.AdminPanel.Application.Payment.Command.CreateProviderPlan;
using Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivateParticipantPlan;
using Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivateProviderPlan;
using Aizen.Bff.AdminPanel.Application.Payment.Command.UpdateParticipantPlan;
using Aizen.Bff.AdminPanel.Application.Payment.Command.UpdateProviderPlan;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetParticipantPlanById;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderPlanById;
using Aizen.Bff.AdminPanel.Application.Payment.Command.CreatePartCommercialTerm;
using Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivatePartCommercialTerm;
using Aizen.Bff.AdminPanel.Application.Payment.Command.ReactivatePartCommercialTerm;
using Aizen.Bff.AdminPanel.Application.Payment.Command.UpdatePartCommercialTerm;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetPartCommercialTermDetail;
using Aizen.Bff.AdminPanel.Application.Payment.Query.GetPartCommercialTermsList;

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

    /// <summary>GET api/v1/admin-panel/payment/subscriptions — paged merged list of provider + participant subscriptions</summary>
    [HttpGet("subscriptions")]
    [ProducesResponseType(typeof(AdminSubscriptionListBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminSubscriptionListBffResult>> GetAdminSubscriptionList(
        [FromQuery] string? audience = null,
        [FromQuery] string? status   = null,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetAdminSubscriptionListBffQuery
        {
            Audience = audience,
            Status   = status,
            Page     = page,
            PageSize = pageSize,
        }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>GET api/v1/admin-panel/payment/subscriptions/mrr-trend — last N months of merged MRR</summary>
    [HttpGet("subscriptions/mrr-trend")]
    [ProducesResponseType(typeof(SubscriptionMrrTrendBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SubscriptionMrrTrendBffResult>> GetSubscriptionMrrTrend(
        [FromQuery] int months = 6,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetSubscriptionMrrTrendBffQuery { Months = months }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>GET api/v1/admin-panel/payment/subscriptions/churn-risk — PastDue + expiring-soon counts</summary>
    [HttpGet("subscriptions/churn-risk")]
    [ProducesResponseType(typeof(SubscriptionChurnRiskBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SubscriptionChurnRiskBffDto>> GetSubscriptionChurnRisk(
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetSubscriptionChurnRiskBffQuery(), ct);
        return SetResponse(result?.Result);
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

    /// <summary>
    /// GET api/v1/admin-panel/payment/commission/rules — paged list with filters.
    /// Phase 13 (July 2026): Extended with CargoDry targeting, labeling, date, and search filters.
    /// </summary>
    [HttpGet("commission/rules")]
    [ProducesResponseType(typeof(CommissionRuleListBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CommissionRuleListBffResult>> GetCommissionRules(
        [FromQuery] string?   ruleType          = null,
        [FromQuery] string?   status            = null,
        [FromQuery] string?   priority          = null,
        [FromQuery] int       page              = 1,
        [FromQuery] int       pageSize          = 20,
        [FromQuery] string?   contextType       = null,
        [FromQuery] string?   commercialModel   = null,
        [FromQuery] string?   productCode       = null,
        [FromQuery] string?   salesChannel      = null,
        [FromQuery] string?   search            = null,
        [FromQuery] long?     providerProfileId = null,
        [FromQuery] DateTime? effectiveOnUtc    = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetCommissionRulesListBffQuery
        {
            RuleType          = ruleType,
            Status            = status,
            Priority          = priority,
            Page              = page,
            PageSize          = pageSize,
            ContextType       = contextType,
            CommercialModel   = commercialModel,
            ProductCode       = productCode,
            SalesChannel      = salesChannel,
            Search            = search,
            ProviderProfileId = providerProfileId,
            EffectiveOnUtc    = effectiveOnUtc,
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

    // ─── Sub-merchant onboarding KYC review (BE-I1) ───────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/providers/sub-merchant/onboarding-queue — paged KYC review queue.</summary>
    [HttpGet("providers/sub-merchant/onboarding-queue")]
    [ProducesResponseType(typeof(ProviderSubMerchantOnboardingQueueDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderSubMerchantOnboardingQueueDto?>> GetSubMerchantOnboardingQueue(
        [FromQuery] ProviderSubMerchantOnboardingStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetSubMerchantOnboardingQueueBffQuery
        {
            Status = status, Page = page, PageSize = pageSize,
        }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>POST api/v1/admin-panel/payment/providers/{id}/sub-merchant/verify — SubMerchantCreated → Verified.</summary>
    [HttpPost("providers/{providerProfileId:long}/sub-merchant/verify")]
    [ProducesResponseType(typeof(ProviderSubMerchantOnboardingResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderSubMerchantOnboardingResult?>> VerifySubMerchant(
        long providerProfileId,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new VerifySubMerchantBffCommand { ProviderProfileId = providerProfileId }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>POST api/v1/admin-panel/payment/providers/{id}/sub-merchant/reject — → Rejected (typed reason body).</summary>
    [HttpPost("providers/{providerProfileId:long}/sub-merchant/reject")]
    [ProducesResponseType(typeof(ProviderSubMerchantOnboardingResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderSubMerchantOnboardingResult?>> RejectSubMerchant(
        long providerProfileId,
        [FromBody] RejectSubMerchantBffRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new RejectSubMerchantBffCommand
        {
            ProviderProfileId = providerProfileId, Reason = body?.Reason,
        }, ct);
        return SetResponse(result?.Result);
    }

    // ─── BE-P3 PlatformFeeRule CRUD ───────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/platform-fee/rules/stats — KPI strip.</summary>
    [HttpGet("platform-fee/rules/stats")]
    [ProducesResponseType(typeof(PlatformFeeRuleStatsBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PlatformFeeRuleStatsBffDto?>> GetPlatformFeeRuleStats(
        CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new GetPlatformFeeRuleStatsBffQuery(), ct))?.Stats);

    /// <summary>GET api/v1/admin-panel/payment/platform-fee/rules — paged list with filters.</summary>
    [HttpGet("platform-fee/rules")]
    [ProducesResponseType(typeof(PlatformFeeRulesListBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PlatformFeeRulesListBffResult>> GetPlatformFeeRules(
        [FromQuery] string?  model        = null,
        [FromQuery] string?  status       = null,
        [FromQuery] string?  currencyCode = null,
        [FromQuery] string?  categoryCode = null,
        [FromQuery] string?  customerType = null,
        [FromQuery] int      page         = 1,
        [FromQuery] int      pageSize     = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetPlatformFeeRulesListBffQuery
        {
            Model        = model,
            Status       = status,
            CurrencyCode = currencyCode,
            CategoryCode = categoryCode,
            CustomerType = customerType,
            Page         = page,
            PageSize     = pageSize,
        }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>GET api/v1/admin-panel/payment/platform-fee/rules/{id} — single rule detail.</summary>
    [HttpGet("platform-fee/rules/{id:long}")]
    [ProducesResponseType(typeof(PlatformFeeRuleDetailBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PlatformFeeRuleDetailBffDto?>> GetPlatformFeeRuleById(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new GetPlatformFeeRuleDetailBffQuery { Id = id }, ct))?.Rule);

    /// <summary>GET api/v1/admin-panel/payment/platform-fee/resolve — point-in-time fee preview.</summary>
    [HttpGet("platform-fee/resolve")]
    [ProducesResponseType(typeof(PlatformFeeResolveBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PlatformFeeResolveBffResult?>> ResolvePlatformFee(
        [FromQuery] string currencyCode = "TRY",
        [FromQuery] string? categoryCode = null,
        [FromQuery] string? customerType = null,
        [FromQuery] decimal customerPayableServiceAmount = 0m,
        CancellationToken ct = default)
    {
        var r = await _cqrs.ProcessAsync(new ResolvePlatformFeeBffQuery
        {
            CurrencyCode = currencyCode, CategoryCode = categoryCode,
            CustomerType = customerType, CustomerPayableServiceAmount = customerPayableServiceAmount,
        }, ct);
        return SetResponse(r?.Result);
    }

    /// <summary>POST api/v1/admin-panel/payment/platform-fee/rules — create a platform-fee rule.</summary>
    [HttpPost("platform-fee/rules")]
    [ProducesResponseType(typeof(PlatformFeeRuleCreateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PlatformFeeRuleCreateBffResult?>> CreatePlatformFeeRule(
        [FromBody] CreatePlatformFeeRuleBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new CreatePlatformFeeRuleBffCommand { Body = body }, ct))?.Result);

    /// <summary>PUT api/v1/admin-panel/payment/platform-fee/rules/{id} — update a platform-fee rule.</summary>
    [HttpPut("platform-fee/rules/{id:long}")]
    [ProducesResponseType(typeof(PlatformFeeRuleMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PlatformFeeRuleMutateBffResult?>> UpdatePlatformFeeRule(
        long id, [FromBody] UpdatePlatformFeeRuleBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new UpdatePlatformFeeRuleBffCommand { Id = id, Body = body }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/platform-fee/rules/{id}/deactivate.</summary>
    [HttpPost("platform-fee/rules/{id:long}/deactivate")]
    [ProducesResponseType(typeof(PlatformFeeRuleMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PlatformFeeRuleMutateBffResult?>> DeactivatePlatformFeeRule(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new DeactivatePlatformFeeRuleBffCommand { Id = id }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/platform-fee/rules/{id}/reactivate — re-activate an Inactive rule.</summary>
    [HttpPost("platform-fee/rules/{id:long}/reactivate")]
    [ProducesResponseType(typeof(PlatformFeeRuleMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PlatformFeeRuleMutateBffResult?>> ReactivatePlatformFeeRule(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new ReactivatePlatformFeeRuleBffCommand { Id = id }, ct))?.Result);

    // ─── BE-P4 ProviderPlanPrice CRUD ─────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/plan-prices/plan/{planId} — all versioned prices for a plan.</summary>
    [HttpGet("plan-prices/plan/{planId:long}")]
    [ProducesResponseType(typeof(List<ProviderPlanPriceBffDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<ProviderPlanPriceBffDto>?>> GetProviderPlanPrices(
        long planId, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new GetProviderPlanPricesBffQuery { ProviderPlanId = planId }, ct))?.Items);

    /// <summary>GET api/v1/admin-panel/payment/plan-prices/resolve — point-in-time price (resolver dev query).</summary>
    [HttpGet("plan-prices/resolve")]
    [ProducesResponseType(typeof(ProviderPlanPriceBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderPlanPriceBffDto?>> ResolveProviderPlanPrice(
        [FromQuery] long providerPlanId,
        [FromQuery] string currencyCode = "TRY",
        [FromQuery] string billingPeriod = "Monthly",
        [FromQuery] DateTime? atUtc = null,
        CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new ResolveProviderPlanPriceBffQuery
        {
            ProviderPlanId = providerPlanId, CurrencyCode = currencyCode, BillingPeriod = billingPeriod, AtUtc = atUtc,
        }, ct))?.Result);

    /// <summary>GET api/v1/admin-panel/payment/plan-prices/upcoming-changes — renewals with an upcoming price change.</summary>
    [HttpGet("plan-prices/upcoming-changes")]
    [ProducesResponseType(typeof(List<UpcomingPriceChangeBffItem>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<UpcomingPriceChangeBffItem>?>> GetUpcomingPlanPriceChanges(
        [FromQuery] int withinDays = 14, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new GetUpcomingPlanPriceChangesBffQuery { WithinDays = withinDays }, ct))?.Items);

    /// <summary>POST api/v1/admin-panel/payment/plan-prices — create a versioned plan price.</summary>
    [HttpPost("plan-prices")]
    [ProducesResponseType(typeof(ProviderPlanPriceCreateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderPlanPriceCreateBffResult?>> CreateProviderPlanPrice(
        [FromBody] CreateProviderPlanPriceBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new CreateProviderPlanPriceBffCommand { Body = body }, ct))?.Result);

    /// <summary>PUT api/v1/admin-panel/payment/plan-prices/{id} — update a plan price.</summary>
    [HttpPut("plan-prices/{id:long}")]
    [ProducesResponseType(typeof(ProviderPlanPriceMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderPlanPriceMutateBffResult?>> UpdateProviderPlanPrice(
        long id, [FromBody] UpdateProviderPlanPriceBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new UpdateProviderPlanPriceBffCommand { Id = id, Body = body }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/plan-prices/{id}/deactivate.</summary>
    [HttpPost("plan-prices/{id:long}/deactivate")]
    [ProducesResponseType(typeof(ProviderPlanPriceMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderPlanPriceMutateBffResult?>> DeactivateProviderPlanPrice(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new DeactivateProviderPlanPriceBffCommand { Id = id }, ct))?.Result);

    // ─── BE-P5 ProfitProtectionPolicy CRUD ────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/profit-protection/resolve — active policy preview.</summary>
    [HttpGet("profit-protection/resolve")]
    [ProducesResponseType(typeof(ProfitProtectionPolicyResolveBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProfitProtectionPolicyResolveBffResult?>> ResolveProfitProtectionPolicy(
        [FromQuery] string currencyCode = "TRY", [FromQuery] DateTime? atUtc = null, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new ResolveProfitProtectionPolicyBffQuery { CurrencyCode = currencyCode, AtUtc = atUtc }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/profit-protection/policies — create a policy.</summary>
    [HttpPost("profit-protection/policies")]
    [ProducesResponseType(typeof(ProfitProtectionPolicyCreateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProfitProtectionPolicyCreateBffResult?>> CreateProfitProtectionPolicy(
        [FromBody] CreateProfitProtectionPolicyBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new CreateProfitProtectionPolicyBffCommand { Body = body }, ct))?.Result);

    /// <summary>PUT api/v1/admin-panel/payment/profit-protection/policies/{id} — update a policy.</summary>
    [HttpPut("profit-protection/policies/{id:long}")]
    [ProducesResponseType(typeof(ProfitProtectionPolicyMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProfitProtectionPolicyMutateBffResult?>> UpdateProfitProtectionPolicy(
        long id, [FromBody] UpdateProfitProtectionPolicyBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new UpdateProfitProtectionPolicyBffCommand { Id = id, Body = body }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/profit-protection/policies/{id}/deactivate.</summary>
    [HttpPost("profit-protection/policies/{id:long}/deactivate")]
    [ProducesResponseType(typeof(ProfitProtectionPolicyMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProfitProtectionPolicyMutateBffResult?>> DeactivateProfitProtectionPolicy(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new DeactivateProfitProtectionPolicyBffCommand { Id = id }, ct))?.Result);

    /// <summary>GET api/v1/admin-panel/payment/profit-protection/policies — version history (no paging), optional filters.</summary>
    [HttpGet("profit-protection/policies")]
    [ProducesResponseType(typeof(ProfitProtectionPolicyListBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProfitProtectionPolicyListBffResult>> GetProfitProtectionPolicies(
        [FromQuery] string? currencyCode = null,
        [FromQuery] string? status       = null,
        [FromQuery] bool?   isActive     = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetProfitProtectionPoliciesListBffQuery
        {
            CurrencyCode = currencyCode,
            Status       = status,
            IsActive     = isActive,
        }, ct);
        return SetResponse(result?.Result ?? new ProfitProtectionPolicyListBffResult(new(), 0));
    }

    /// <summary>GET api/v1/admin-panel/payment/profit-protection/policies/{id} — single policy detail.</summary>
    [HttpGet("profit-protection/policies/{id:long}")]
    [ProducesResponseType(typeof(ProfitProtectionPolicyDetailBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProfitProtectionPolicyDetailBffDto?>> GetProfitProtectionPolicyById(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new GetProfitProtectionPolicyDetailBffQuery { Id = id }, ct))?.Policy);

    /// <summary>POST api/v1/admin-panel/payment/profit-protection/policies/{id}/reactivate — re-activate an Inactive policy.</summary>
    [HttpPost("profit-protection/policies/{id:long}/reactivate")]
    [ProducesResponseType(typeof(ProfitProtectionPolicyMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProfitProtectionPolicyMutateBffResult?>> ReactivateProfitProtectionPolicy(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new ReactivateProfitProtectionPolicyBffCommand { Id = id }, ct))?.Result);

    // ─── BE-P6 CustomerDiscountRule + CustomerBenefitBudgetPolicy CRUD ─────────

    /// <summary>GET api/v1/admin-panel/payment/customer-discounts/resolve — discount + funding preview.</summary>
    [HttpGet("customer-discounts/resolve")]
    [ProducesResponseType(typeof(CustomerDiscountResolveBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CustomerDiscountResolveBffResult?>> ResolveCustomerDiscount(
        [FromQuery] long? customerPlanId = null,
        [FromQuery] string? categoryCode = null,
        [FromQuery] string currencyCode = "TRY",
        [FromQuery] decimal serviceBaseAmount = 0m,
        [FromQuery] bool providerConsent = false,
        [FromQuery] long? participantPlanSubscriptionId = null,
        CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new ResolveCustomerDiscountBffQuery
        {
            CustomerPlanId = customerPlanId, CategoryCode = categoryCode, CurrencyCode = currencyCode,
            ServiceBaseAmount = serviceBaseAmount, ProviderConsent = providerConsent,
            ParticipantPlanSubscriptionId = participantPlanSubscriptionId,
        }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/customer-discounts/rules — create a discount rule.</summary>
    [HttpPost("customer-discounts/rules")]
    [ProducesResponseType(typeof(CustomerDiscountRuleCreateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CustomerDiscountRuleCreateBffResult?>> CreateCustomerDiscountRule(
        [FromBody] CreateCustomerDiscountRuleBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new CreateCustomerDiscountRuleBffCommand { Body = body }, ct))?.Result);

    /// <summary>PUT api/v1/admin-panel/payment/customer-discounts/rules/{id} — update a discount rule.</summary>
    [HttpPut("customer-discounts/rules/{id:long}")]
    [ProducesResponseType(typeof(CustomerDiscountRuleMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CustomerDiscountRuleMutateBffResult?>> UpdateCustomerDiscountRule(
        long id, [FromBody] UpdateCustomerDiscountRuleBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new UpdateCustomerDiscountRuleBffCommand { Id = id, Body = body }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/customer-discounts/rules/{id}/deactivate.</summary>
    [HttpPost("customer-discounts/rules/{id:long}/deactivate")]
    [ProducesResponseType(typeof(CustomerDiscountRuleMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CustomerDiscountRuleMutateBffResult?>> DeactivateCustomerDiscountRule(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new DeactivateCustomerDiscountRuleBffCommand { Id = id }, ct))?.Result);

    /// <summary>GET api/v1/admin-panel/payment/customer-discounts/rules — rule list (no paging), optional filters.</summary>
    [HttpGet("customer-discounts/rules")]
    [ProducesResponseType(typeof(CustomerDiscountRuleListBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CustomerDiscountRuleListBffResult>> GetCustomerDiscountRules(
        [FromQuery] long?   customerPlanId = null,
        [FromQuery] string? categoryCode   = null,
        [FromQuery] string? currencyCode   = null,
        [FromQuery] string? fundingMode    = null,
        [FromQuery] bool?   isActive       = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetCustomerDiscountRulesListBffQuery
        {
            CustomerPlanId = customerPlanId,
            CategoryCode   = categoryCode,
            CurrencyCode   = currencyCode,
            FundingMode    = fundingMode,
            IsActive       = isActive,
        }, ct);
        return SetResponse(result?.Result ?? new CustomerDiscountRuleListBffResult(new(), 0));
    }

    /// <summary>GET api/v1/admin-panel/payment/customer-discounts/rules/{id} — single rule detail.</summary>
    [HttpGet("customer-discounts/rules/{id:long}")]
    [ProducesResponseType(typeof(CustomerDiscountRuleDetailBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CustomerDiscountRuleDetailBffDto?>> GetCustomerDiscountRuleById(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new GetCustomerDiscountRuleDetailBffQuery { Id = id }, ct))?.Rule);

    /// <summary>POST api/v1/admin-panel/payment/customer-discounts/rules/{id}/reactivate — re-activate an Inactive rule.</summary>
    [HttpPost("customer-discounts/rules/{id:long}/reactivate")]
    [ProducesResponseType(typeof(CustomerDiscountRuleMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CustomerDiscountRuleMutateBffResult?>> ReactivateCustomerDiscountRule(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new ReactivateCustomerDiscountRuleBffCommand { Id = id }, ct))?.Result);

    // ─── BE-S5 PartCommercialTerm (admin-only, cost-bearing) ─────────────────────

    /// <summary>GET api/v1/admin-panel/payment/part-commercial-term/rules — term list (no paging), optional scope filters.</summary>
    [HttpGet("part-commercial-term/rules")]
    [ProducesResponseType(typeof(PartCommercialTermListBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PartCommercialTermListBffResult>> GetPartCommercialTerms(
        [FromQuery] string? brand             = null,
        [FromQuery] string? productCode       = null,
        [FromQuery] long?   providerProfileId = null,
        [FromQuery] string? categoryCode      = null,
        [FromQuery] string? currencyCode      = null,
        [FromQuery] bool?   isActive          = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetPartCommercialTermsListBffQuery
        {
            Brand             = brand,
            ProductCode       = productCode,
            ProviderProfileId = providerProfileId,
            CategoryCode      = categoryCode,
            CurrencyCode      = currencyCode,
            IsActive          = isActive,
        }, ct);
        return SetResponse(result?.Result ?? new PartCommercialTermListBffResult(new(), 0));
    }

    /// <summary>GET api/v1/admin-panel/payment/part-commercial-term/rules/{id} — single term detail (carries cost).</summary>
    [HttpGet("part-commercial-term/rules/{id:long}")]
    [ProducesResponseType(typeof(PartCommercialTermBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PartCommercialTermBffDto?>> GetPartCommercialTermById(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new GetPartCommercialTermDetailBffQuery { Id = id }, ct))?.Term);

    /// <summary>POST api/v1/admin-panel/payment/part-commercial-term/rules — create (append a new version of) a term.</summary>
    [HttpPost("part-commercial-term/rules")]
    [ProducesResponseType(typeof(PartCommercialTermCreateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PartCommercialTermCreateBffResult?>> CreatePartCommercialTerm(
        [FromBody] CreatePartCommercialTermBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new CreatePartCommercialTermBffCommand { Body = body }, ct))?.Result);

    /// <summary>PUT api/v1/admin-panel/payment/part-commercial-term/rules/{id} — update a term in place (scope/version immutable).</summary>
    [HttpPut("part-commercial-term/rules/{id:long}")]
    [ProducesResponseType(typeof(PartCommercialTermUpdateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PartCommercialTermUpdateBffResult?>> UpdatePartCommercialTerm(
        long id, [FromBody] UpdatePartCommercialTermBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new UpdatePartCommercialTermBffCommand { Id = id, Body = body }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/part-commercial-term/rules/{id}/deactivate.</summary>
    [HttpPost("part-commercial-term/rules/{id:long}/deactivate")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PartCommercialTermDeactivateResult?>> DeactivatePartCommercialTerm(
        long id, CancellationToken ct = default)
        => SetResponse(new PartCommercialTermDeactivateResult((await _cqrs.ProcessAsync(new DeactivatePartCommercialTermBffCommand { Id = id }, ct))?.Result ?? false));

    /// <summary>POST api/v1/admin-panel/payment/part-commercial-term/rules/{id}/reactivate — re-activate an Inactive term.</summary>
    [HttpPost("part-commercial-term/rules/{id:long}/reactivate")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PartCommercialTermDeactivateResult?>> ReactivatePartCommercialTerm(
        long id, CancellationToken ct = default)
        => SetResponse(new PartCommercialTermDeactivateResult((await _cqrs.ProcessAsync(new ReactivatePartCommercialTermBffCommand { Id = id }, ct))?.Result ?? false));

    /// <summary>POST api/v1/admin-panel/payment/benefit-budget/policies — create a per-plan budget policy.</summary>
    [HttpPost("benefit-budget/policies")]
    [ProducesResponseType(typeof(CustomerBenefitBudgetPolicyCreateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CustomerBenefitBudgetPolicyCreateBffResult?>> CreateCustomerBenefitBudgetPolicy(
        [FromBody] CreateCustomerBenefitBudgetPolicyBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new CreateCustomerBenefitBudgetPolicyBffCommand { Body = body }, ct))?.Result);

    /// <summary>GET api/v1/admin-panel/payment/benefit-budget/policies — per-plan policy list (no paging), optional filters.</summary>
    [HttpGet("benefit-budget/policies")]
    [ProducesResponseType(typeof(CustomerBenefitBudgetPolicyListBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CustomerBenefitBudgetPolicyListBffResult>> GetCustomerBenefitBudgetPolicies(
        [FromQuery] long?   customerPlanId = null,
        [FromQuery] string? currencyCode   = null,
        [FromQuery] bool?   isActive       = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetCustomerBenefitBudgetPoliciesListBffQuery
        {
            CustomerPlanId = customerPlanId,
            CurrencyCode   = currencyCode,
            IsActive       = isActive,
        }, ct);
        return SetResponse(result?.Result ?? new CustomerBenefitBudgetPolicyListBffResult(new(), 0));
    }

    /// <summary>GET api/v1/admin-panel/payment/benefit-budget/policies/{id} — single policy detail.</summary>
    [HttpGet("benefit-budget/policies/{id:long}")]
    [ProducesResponseType(typeof(CustomerBenefitBudgetPolicyDetailBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CustomerBenefitBudgetPolicyDetailBffDto?>> GetCustomerBenefitBudgetPolicyById(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new GetCustomerBenefitBudgetPolicyDetailBffQuery { Id = id }, ct))?.Policy);

    // ─── BE-P7 ProviderCommissionBenefitRule + entitlement ────────────────────

    /// <summary>GET api/v1/admin-panel/payment/commission-benefits/resolve — effective-commission preview.</summary>
    [HttpGet("commission-benefits/resolve")]
    [ProducesResponseType(typeof(EffectiveCommissionResolveBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<EffectiveCommissionResolveBffResult?>> ResolveEffectiveCommission(
        [FromQuery] long providerProfileId,
        [FromQuery] long? providerPlanId = null,
        [FromQuery] string? categoryCode = null,
        [FromQuery] decimal serviceAmount = 0m,
        [FromQuery] string currencyCode = "TRY",
        [FromQuery] decimal? eligibleGmvRemaining = null,
        [FromQuery] decimal planFloorRate = 0m,
        CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new ResolveEffectiveCommissionBffQuery
        {
            ProviderProfileId = providerProfileId, ProviderPlanId = providerPlanId, CategoryCode = categoryCode,
            ServiceAmount = serviceAmount, CurrencyCode = currencyCode,
            EligibleGmvRemaining = eligibleGmvRemaining, PlanFloorRate = planFloorRate,
        }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/commission-benefits/rules — create a benefit rule.</summary>
    [HttpPost("commission-benefits/rules")]
    [ProducesResponseType(typeof(ProviderCommissionBenefitRuleCreateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderCommissionBenefitRuleCreateBffResult?>> CreateProviderCommissionBenefitRule(
        [FromBody] CreateProviderCommissionBenefitRuleBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new CreateProviderCommissionBenefitRuleBffCommand { Body = body }, ct))?.Result);

    /// <summary>PUT api/v1/admin-panel/payment/commission-benefits/rules/{id} — update a benefit rule.</summary>
    [HttpPut("commission-benefits/rules/{id:long}")]
    [ProducesResponseType(typeof(ProviderCommissionBenefitRuleMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderCommissionBenefitRuleMutateBffResult?>> UpdateProviderCommissionBenefitRule(
        long id, [FromBody] UpdateProviderCommissionBenefitRuleBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new UpdateProviderCommissionBenefitRuleBffCommand { Id = id, Body = body }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/commission-benefits/rules/{id}/deactivate.</summary>
    [HttpPost("commission-benefits/rules/{id:long}/deactivate")]
    [ProducesResponseType(typeof(ProviderCommissionBenefitRuleMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderCommissionBenefitRuleMutateBffResult?>> DeactivateProviderCommissionBenefitRule(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new DeactivateProviderCommissionBenefitRuleBffCommand { Id = id }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/commission-benefits/entitlements — grant an entitlement.</summary>
    [HttpPost("commission-benefits/entitlements")]
    [ProducesResponseType(typeof(GrantEntitlementBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GrantEntitlementBffResult?>> GrantCommissionBenefitEntitlement(
        [FromBody] GrantProviderCommissionBenefitEntitlementBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new GrantProviderCommissionBenefitEntitlementBffCommand { Body = body }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/commission-benefits/entitlements/{id}/revoke.</summary>
    [HttpPost("commission-benefits/entitlements/{id:long}/revoke")]
    [ProducesResponseType(typeof(RevokeEntitlementBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RevokeEntitlementBffResult?>> RevokeCommissionBenefitEntitlement(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new RevokeProviderCommissionBenefitEntitlementBffCommand { Id = id }, ct))?.Result);

    /// <summary>GET api/v1/admin-panel/payment/commission-benefits/rules — benefit rule list (no paging), optional filters.</summary>
    [HttpGet("commission-benefits/rules")]
    [ProducesResponseType(typeof(ProviderCommissionBenefitRuleListBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderCommissionBenefitRuleListBffResult>> GetProviderCommissionBenefitRules(
        [FromQuery] long?   providerProfileId = null,
        [FromQuery] long?   providerPlanId    = null,
        [FromQuery] string? categoryCode      = null,
        [FromQuery] string? currencyCode      = null,
        [FromQuery] bool?   stackable         = null,
        [FromQuery] bool?   isActive          = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetProviderCommissionBenefitRulesListBffQuery
        {
            ProviderProfileId = providerProfileId,
            ProviderPlanId    = providerPlanId,
            CategoryCode      = categoryCode,
            CurrencyCode      = currencyCode,
            Stackable         = stackable,
            IsActive          = isActive,
        }, ct);
        return SetResponse(result ?? new ProviderCommissionBenefitRuleListBffResult(new(), 0));
    }

    /// <summary>GET api/v1/admin-panel/payment/commission-benefits/rules/{id} — single benefit rule detail.</summary>
    [HttpGet("commission-benefits/rules/{id:long}")]
    [ProducesResponseType(typeof(ProviderCommissionBenefitRuleDetailBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderCommissionBenefitRuleDetailBffDto?>> GetProviderCommissionBenefitRuleById(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new GetProviderCommissionBenefitRuleDetailBffQuery { Id = id }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/commission-benefits/rules/{id}/reactivate — re-activate an Inactive benefit rule.</summary>
    [HttpPost("commission-benefits/rules/{id:long}/reactivate")]
    [ProducesResponseType(typeof(ProviderCommissionBenefitRuleMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderCommissionBenefitRuleMutateBffResult?>> ReactivateProviderCommissionBenefitRule(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new ReactivateProviderCommissionBenefitRuleBffCommand { Id = id }, ct))?.Result);

    /// <summary>GET api/v1/admin-panel/payment/commission-benefits/entitlements — granted entitlement list (no paging).</summary>
    [HttpGet("commission-benefits/entitlements")]
    [ProducesResponseType(typeof(ProviderCommissionBenefitEntitlementListBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderCommissionBenefitEntitlementListBffResult>> GetProviderCommissionBenefitEntitlements(
        [FromQuery] long? providerProfileId = null,
        [FromQuery] long? benefitRuleId     = null,
        [FromQuery] bool? isActive          = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetProviderCommissionBenefitEntitlementsListBffQuery
        {
            ProviderProfileId = providerProfileId,
            BenefitRuleId     = benefitRuleId,
            IsActive          = isActive,
        }, ct);
        return SetResponse(result ?? new ProviderCommissionBenefitEntitlementListBffResult(new(), 0));
    }

    /// <summary>GET api/v1/admin-panel/payment/commission-benefits/entitlements/{id} — single entitlement detail.</summary>
    [HttpGet("commission-benefits/entitlements/{id:long}")]
    [ProducesResponseType(typeof(ProviderCommissionBenefitEntitlementDetailBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderCommissionBenefitEntitlementDetailBffDto?>> GetProviderCommissionBenefitEntitlementById(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new GetProviderCommissionBenefitEntitlementDetailBffQuery { Id = id }, ct))?.Result);

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

    // ─── Provider Plans — CRUD ────────────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/provider-plans/{id}</summary>
    [HttpGet("provider-plans/{id:long}")]
    [ProducesResponseType(typeof(ProviderPlanBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderPlanBffDto?>> GetProviderPlanById(
        long id,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetProviderPlanByIdBffQuery { Id = id }, ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/payment/provider-plans</summary>
    [HttpPost("provider-plans")]
    [ProducesResponseType(typeof(PlanMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PlanMutateBffResult?>> CreateProviderPlan(
        [FromBody] CreateProviderPlanBffCommand command,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    /// <summary>PUT api/v1/admin-panel/payment/provider-plans/{id}</summary>
    [HttpPut("provider-plans/{id:long}")]
    [ProducesResponseType(typeof(PlanMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PlanMutateBffResult?>> UpdateProviderPlan(
        long id,
        [FromBody] UpdateProviderPlanBffCommand command,
        CancellationToken ct = default)
    {
        var enriched = new UpdateProviderPlanBffCommand
        {
            Id               = id,
            Name             = command.Name,
            Description      = command.Description,
            MonthlyPriceTRY  = command.MonthlyPriceTRY,
            AnnualPriceTRY   = command.AnnualPriceTRY,
            TrialDays        = command.TrialDays,
            BadgeLabel       = command.BadgeLabel,
            MaxActiveOffers  = command.MaxActiveOffers,
            HasPriorityBoost = command.HasPriorityBoost,
            HasFullAnalytics = command.HasFullAnalytics,
            SortOrder        = command.SortOrder,
            ValidFrom        = command.ValidFrom,
            ValidTo          = command.ValidTo,
            FeatureItems     = command.FeatureItems,
        };
        var result = await _cqrs.ProcessAsync(enriched, ct);
        return SetResponse(result);
    }

    // ─── Participant Plans — CRUD ─────────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/participant-plans/{id}</summary>
    [HttpGet("participant-plans/{id:long}")]
    [ProducesResponseType(typeof(ParticipantPlanBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ParticipantPlanBffDto?>> GetParticipantPlanById(
        long id,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetParticipantPlanByIdBffQuery { Id = id }, ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/payment/participant-plans</summary>
    [HttpPost("participant-plans")]
    [ProducesResponseType(typeof(PlanMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PlanMutateBffResult?>> CreateParticipantPlan(
        [FromBody] CreateParticipantPlanBffCommand command,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    /// <summary>PUT api/v1/admin-panel/payment/participant-plans/{id}</summary>
    [HttpPut("participant-plans/{id:long}")]
    [ProducesResponseType(typeof(PlanMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PlanMutateBffResult?>> UpdateParticipantPlan(
        long id,
        [FromBody] UpdateParticipantPlanBffCommand command,
        CancellationToken ct = default)
    {
        var enriched = new UpdateParticipantPlanBffCommand
        {
            Id                    = id,
            Name                  = command.Name,
            Description           = command.Description,
            MonthlyPriceTRY       = command.MonthlyPriceTRY,
            AnnualPriceTRY        = command.AnnualPriceTRY,
            TrialDays             = command.TrialDays,
            BadgeLabel            = command.BadgeLabel,
            ServiceDiscountRate   = command.ServiceDiscountRate,
            CargoDryDiscountRate  = command.CargoDryDiscountRate,
            InkCoinEarnMultiplier = command.InkCoinEarnMultiplier,
            SortOrder             = command.SortOrder,
            ValidFrom             = command.ValidFrom,
            ValidTo               = command.ValidTo,
            FeatureItems          = command.FeatureItems,
        };
        var result = await _cqrs.ProcessAsync(enriched, ct);
        return SetResponse(result);
    }

    // ─── Plans — Activate / Deactivate ───────────────────────────────────────

    /// <summary>POST api/v1/admin-panel/payment/provider-plans/{id}/activate</summary>
    [HttpPost("provider-plans/{id:long}/activate")]
    [ProducesResponseType(typeof(PlanMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PlanMutateBffResult?>> ActivateProviderPlan(
        long id,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new ActivateProviderPlanBffCommand { Id = id }, ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/payment/provider-plans/{id}/deactivate</summary>
    [HttpPost("provider-plans/{id:long}/deactivate")]
    [ProducesResponseType(typeof(PlanMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PlanMutateBffResult?>> DeactivateProviderPlan(
        long id,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new DeactivateProviderPlanBffCommand { Id = id }, ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/payment/participant-plans/{id}/activate</summary>
    [HttpPost("participant-plans/{id:long}/activate")]
    [ProducesResponseType(typeof(PlanMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PlanMutateBffResult?>> ActivateParticipantPlan(
        long id,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new ActivateParticipantPlanBffCommand { Id = id }, ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/payment/participant-plans/{id}/deactivate</summary>
    [HttpPost("participant-plans/{id:long}/deactivate")]
    [ProducesResponseType(typeof(PlanMutateBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PlanMutateBffResult?>> DeactivateParticipantPlan(
        long id,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new DeactivateParticipantPlanBffCommand { Id = id }, ct);
        return SetResponse(result);
    }

    // ─── P11 Premium admin — products ─────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/admin/premium/products — all premium products.</summary>
    [HttpGet("admin/premium/products")]
    [ProducesResponseType(typeof(List<PremiumProductAdminDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<PremiumProductAdminDto>?>> GetPremiumProducts(
        CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new GetPremiumProductsBffQuery(), ct))?.Items);

    /// <summary>GET api/v1/admin-panel/payment/admin/premium/products/{id}.</summary>
    [HttpGet("admin/premium/products/{id:long}")]
    [ProducesResponseType(typeof(PremiumProductAdminDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PremiumProductAdminDto?>> GetPremiumProductById(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new GetPremiumProductByIdBffQuery { Id = id }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/admin/premium/products — create a premium product.</summary>
    [HttpPost("admin/premium/products")]
    [ProducesResponseType(typeof(PremiumMutateResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PremiumMutateResultDto?>> CreatePremiumProduct(
        [FromBody] CreatePremiumProductBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new CreatePremiumProductBffCommand { Body = body }, ct))?.Result);

    /// <summary>PUT api/v1/admin-panel/payment/admin/premium/products/{id} — update a premium product.</summary>
    [HttpPut("admin/premium/products/{id:long}")]
    [ProducesResponseType(typeof(PremiumMutateResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PremiumMutateResultDto?>> UpdatePremiumProduct(
        long id, [FromBody] UpdatePremiumProductBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new UpdatePremiumProductBffCommand { Id = id, Body = body }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/admin/premium/products/{id}/activate.</summary>
    [HttpPost("admin/premium/products/{id:long}/activate")]
    [ProducesResponseType(typeof(PremiumMutateResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PremiumMutateResultDto?>> ActivatePremiumProduct(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new ActivatePremiumProductBffCommand { Id = id }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/admin/premium/products/{id}/deactivate.</summary>
    [HttpPost("admin/premium/products/{id:long}/deactivate")]
    [ProducesResponseType(typeof(PremiumMutateResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PremiumMutateResultDto?>> DeactivatePremiumProduct(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new DeactivatePremiumProductBffCommand { Id = id }, ct))?.Result);

    // ─── P11 Premium admin — prices ───────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/admin/premium/products/{productId}/prices — versioned prices.</summary>
    [HttpGet("admin/premium/products/{productId:long}/prices")]
    [ProducesResponseType(typeof(List<PremiumProductPriceAdminDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<PremiumProductPriceAdminDto>?>> GetPremiumProductPrices(
        long productId, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new GetPremiumProductPricesBffQuery { ProductId = productId }, ct))?.Items);

    /// <summary>GET api/v1/admin-panel/payment/admin/premium/products/{productId}/prices/resolve — point-in-time price.</summary>
    [HttpGet("admin/premium/products/{productId:long}/prices/resolve")]
    [ProducesResponseType(typeof(PremiumProductPriceAdminDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PremiumProductPriceAdminDto?>> ResolvePremiumProductPrice(
        long productId,
        [FromQuery] string currencyCode = "TRY",
        [FromQuery] DateTime? atUtc = null,
        CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new ResolvePremiumProductPriceBffQuery
        {
            ProductId = productId, CurrencyCode = currencyCode, AtUtc = atUtc,
        }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/admin/premium/prices — create a versioned premium price.</summary>
    [HttpPost("admin/premium/prices")]
    [ProducesResponseType(typeof(PremiumMutateResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PremiumMutateResultDto?>> CreatePremiumProductPrice(
        [FromBody] CreatePremiumProductPriceBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new CreatePremiumProductPriceBffCommand { Body = body }, ct))?.Result);

    /// <summary>PUT api/v1/admin-panel/payment/admin/premium/prices/{id} — update a premium price.</summary>
    [HttpPut("admin/premium/prices/{id:long}")]
    [ProducesResponseType(typeof(PremiumMutateResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PremiumMutateResultDto?>> UpdatePremiumProductPrice(
        long id, [FromBody] UpdatePremiumProductPriceBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new UpdatePremiumProductPriceBffCommand { Id = id, Body = body }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/admin/premium/prices/{id}/deactivate.</summary>
    [HttpPost("admin/premium/prices/{id:long}/deactivate")]
    [ProducesResponseType(typeof(PremiumMutateResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PremiumMutateResultDto?>> DeactivatePremiumProductPrice(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new DeactivatePremiumProductPriceBffCommand { Id = id }, ct))?.Result);

    // ─── P10 RefundAllocationPolicy ───────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/admin/refund-allocation-policies — all policies.</summary>
    [HttpGet("admin/refund-allocation-policies")]
    [ProducesResponseType(typeof(List<RefundAllocationPolicyAdminDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<RefundAllocationPolicyAdminDto>?>> GetRefundAllocationPolicies(
        CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new GetRefundAllocationPoliciesBffQuery(), ct))?.Items);

    /// <summary>GET api/v1/admin-panel/payment/admin/refund-allocation-policies/resolve — active policy preview.</summary>
    [HttpGet("admin/refund-allocation-policies/resolve")]
    [ProducesResponseType(typeof(RefundAllocationPolicyAdminDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RefundAllocationPolicyAdminDto?>> ResolveRefundAllocationPolicy(
        [FromQuery] string currencyCode = "TRY",
        [FromQuery] DateTime? atUtc = null,
        CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new ResolveRefundAllocationPolicyBffQuery { CurrencyCode = currencyCode, AtUtc = atUtc }, ct))?.Result);

    /// <summary>GET api/v1/admin-panel/payment/admin/refund-allocation-policies/{id}.</summary>
    [HttpGet("admin/refund-allocation-policies/{id:long}")]
    [ProducesResponseType(typeof(RefundAllocationPolicyAdminDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RefundAllocationPolicyAdminDto?>> GetRefundAllocationPolicyById(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new GetRefundAllocationPolicyByIdBffQuery { Id = id }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/admin/refund-allocation-policies — create a policy.</summary>
    [HttpPost("admin/refund-allocation-policies")]
    [ProducesResponseType(typeof(RefundAllocationPolicyMutateResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RefundAllocationPolicyMutateResultDto?>> CreateRefundAllocationPolicy(
        [FromBody] CreateRefundAllocationPolicyBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new CreateRefundAllocationPolicyBffCommand { Body = body }, ct))?.Result);

    /// <summary>PUT api/v1/admin-panel/payment/admin/refund-allocation-policies/{id} — update a policy.</summary>
    [HttpPut("admin/refund-allocation-policies/{id:long}")]
    [ProducesResponseType(typeof(RefundAllocationPolicyMutateResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RefundAllocationPolicyMutateResultDto?>> UpdateRefundAllocationPolicy(
        long id, [FromBody] UpdateRefundAllocationPolicyBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new UpdateRefundAllocationPolicyBffCommand { Id = id, Body = body }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/admin/refund-allocation-policies/{id}/deactivate.</summary>
    [HttpPost("admin/refund-allocation-policies/{id:long}/deactivate")]
    [ProducesResponseType(typeof(RefundAllocationPolicyMutateResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RefundAllocationPolicyMutateResultDto?>> DeactivateRefundAllocationPolicy(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new DeactivateRefundAllocationPolicyBffCommand { Id = id }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/admin/refund-allocation-policies/{id}/reactivate.</summary>
    [HttpPost("admin/refund-allocation-policies/{id:long}/reactivate")]
    [ProducesResponseType(typeof(RefundAllocationPolicyMutateResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RefundAllocationPolicyMutateResultDto?>> ReactivateRefundAllocationPolicy(
        long id, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new ReactivateRefundAllocationPolicyBffCommand { Id = id }, ct))?.Result);

    // ─── P10 Refund / Chargeback queues ───────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/admin/refund-queue — paged refund queue.</summary>
    [HttpGet("admin/refund-queue")]
    [ProducesResponseType(typeof(RefundQueuePagedDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RefundQueuePagedDto?>> GetRefundQueue(
        [FromQuery] RefundCause? cause = null,
        [FromQuery] ReleaseState? releaseState = null,
        [FromQuery] TransactionRefundStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new GetRefundQueueBffQuery
        {
            Cause = cause, ReleaseState = releaseState, Status = status, Page = page, PageSize = pageSize,
        }, ct))?.Result);

    /// <summary>GET api/v1/admin-panel/payment/admin/chargeback-queue — paged chargeback queue.</summary>
    [HttpGet("admin/chargeback-queue")]
    [ProducesResponseType(typeof(ChargebackQueuePagedDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ChargebackQueuePagedDto?>> GetChargebackQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new GetChargebackQueueBffQuery { Page = page, PageSize = pageSize }, ct))?.Result);

    // ─── P10 ProviderBalance ledger ───────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/payment/admin/provider-balances — paged provider-balance ledger.</summary>
    [HttpGet("admin/provider-balances")]
    [ProducesResponseType(typeof(ProviderBalancePagedDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderBalancePagedDto?>> GetProviderBalances(
        [FromQuery] string? currency = null,
        [FromQuery] bool onlyNegative = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new GetProviderBalancesBffQuery
        {
            Currency = currency, OnlyNegative = onlyNegative, Page = page, PageSize = pageSize,
        }, ct))?.Result);

    /// <summary>GET api/v1/admin-panel/payment/admin/provider-balances/{providerProfileId} — single provider balance.</summary>
    [HttpGet("admin/provider-balances/{providerProfileId:long}")]
    [ProducesResponseType(typeof(ProviderBalanceAdminDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderBalanceAdminDto?>> GetProviderBalance(
        long providerProfileId,
        [FromQuery] string currency = "TRY",
        CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new GetProviderBalanceBffQuery { ProviderProfileId = providerProfileId, Currency = currency }, ct))?.Result);

    /// <summary>POST api/v1/admin-panel/payment/admin/provider-balances/{providerProfileId}/adjust — manual adjustment.</summary>
    [HttpPost("admin/provider-balances/{providerProfileId:long}/adjust")]
    [ProducesResponseType(typeof(ProviderBalanceAdjustResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderBalanceAdjustResultDto?>> AdjustProviderBalance(
        long providerProfileId, [FromBody] AdjustProviderBalanceBffRequest body, CancellationToken ct = default)
        => SetResponse((await _cqrs.ProcessAsync(new AdjustProviderBalanceBffCommand { ProviderProfileId = providerProfileId, Body = body }, ct))?.Result);

}
