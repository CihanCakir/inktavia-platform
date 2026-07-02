using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Request;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.Requests;
using Refit;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Admin payment BFF remote call",
    "Defines BFF-to-Payment module calls for transaction monitoring, payout management, " +
    "subscription oversight, plan reads, commission lookup, and invoice management. " +
    "Auth headers (Authorization + X-Aizen-User-Token) are injected automatically " +
    "by AdminPanelBffAuthDelegatingHandler.")]
public interface IAdminPaymentBffRemoteCall : IAizenRemoteCall
{
    // ─── Transactions — read ──────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/payment/transactions")]
    Task<PaymentTransactionListBffResult> GetTransactionsAsync(
        [Query] PaymentTransactionStatus? status   = null,
        [Query] TransactionType?          type     = null,
        [Query] string?                   gateway  = null,
        [Query] DateTime?                 fromDate = null,
        [Query] DateTime?                 toDate   = null,
        [Query] string?                   search   = null,
        [Query] int                       page     = 1,
        [Query] int                       pageSize = 25,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/transactions/{id}")]
    Task<PaymentTransactionBffDto?> GetTransactionAsync(
        long id,
        CancellationToken ct = default);

    // ─── Transactions — admin operations ─────────────────────────────────────

    [AizenRemoteCallPost("/api/v1/payment/admin/escrow")]
    Task<CreatePaymentEscrowResult> CreateEscrowAsync(
        [AizenRemoteCallBody] CreateEscrowRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/admin/transactions/{id}/capture")]
    Task<CapturePaymentResult> CaptureAsync(
        long id,
        [AizenRemoteCallBody] CapturePaymentRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/admin/transactions/{id}/release")]
    Task<ReleasePaymentEscrowResult> ReleaseEscrowAsync(
        long id,
        [AizenRemoteCallBody] ReleaseEscrowRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/admin/transactions/{id}/refund")]
    Task<RefundPaymentResult> RefundAsync(
        long id,
        [AizenRemoteCallBody] RefundPaymentRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/admin/transactions/{id}/cancel")]
    Task<CancelPaymentResult> CancelTransactionAsync(
        long id,
        [AizenRemoteCallBody] CancelPaymentRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/admin/transactions/{id}/reinstate")]
    Task<ReinstateCancelledTransactionResult> ReinstateAsync(
        long id,
        [AizenRemoteCallBody] ReinstatePaymentRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/admin/refund-records/{refundRecordId}/reverse")]
    Task<ReversePartialRefundResult> ReverseRefundAsync(
        long refundRecordId,
        [AizenRemoteCallBody] ReverseRefundRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/admin/transactions/{id}/refund-history")]
    Task<List<TransactionRefundRecordBffDto>> GetRefundHistoryAsync(
        long id,
        CancellationToken ct = default);

    // ─── Payouts ──────────────────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/payment/payouts/pending")]
    Task<List<PendingPayoutBffDto>> GetPendingPayoutsAsync(
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/payouts/{id}/complete")]
    Task<MarkPayoutCompleteResult> MarkPayoutCompleteAsync(
        long id,
        [AizenRemoteCallBody] MarkPayoutCompleteRequest body,
        CancellationToken ct = default);

    // ─── Subscriptions ────────────────────────────────────────────────────────

    [AizenRemoteCallPost("/api/v1/payment/subscriptions/provider")]
    Task<SubscribeProviderPlanResult> SubscribeProviderAsync(
        [AizenRemoteCallBody] SubscribeProviderPlanRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/subscriptions/provider/{providerProfileId}")]
    Task<ActiveProviderSubscriptionResult?> GetProviderSubscriptionAsync(
        long providerProfileId,
        CancellationToken ct = default);

    [AizenRemoteCallDelete("/api/v1/payment/subscriptions/provider/{providerProfileId}")]
    Task<CancelSubscriptionResult> CancelProviderSubscriptionAsync(
        long    providerProfileId,
        [Query] string? reason = null,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/subscriptions/participant")]
    Task<SubscribeParticipantPlanResult> SubscribeParticipantAsync(
        [AizenRemoteCallBody] SubscribeParticipantPlanRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/subscriptions/participant/{participantProfileId}")]
    Task<ActiveParticipantSubscriptionResult?> GetParticipantSubscriptionAsync(
        long participantProfileId,
        CancellationToken ct = default);

    [AizenRemoteCallDelete("/api/v1/payment/subscriptions/participant/{participantProfileId}")]
    Task<CancelSubscriptionResult> CancelParticipantSubscriptionAsync(
        long    participantProfileId,
        [Query] string? reason = null,
        CancellationToken ct = default);

    // ─── Plans ────────────────────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/payment/provider-plans")]
    Task<List<ProviderPlanBffDto>> GetProviderPlansAsync(
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/participant-plans")]
    Task<List<ParticipantPlanBffDto>> GetParticipantPlansAsync(
        CancellationToken ct = default);

    // ─── Commission ───────────────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/payment/commission/resolve")]
    Task<CommissionRateBffResult> ResolveCommissionRateAsync(
        [Query] long?   providerProfileId = null,
        [Query] long?   providerPlanId    = null,
        [Query] string? categoryCode      = null,
        CancellationToken ct = default);

    // ─── Commission Rules — CRUD ──────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/payment/commission/rules")]
    Task<CommissionRuleListBffResult> GetCommissionRulesPagedAsync(
        [Query] string? ruleType  = null,
        [Query] string? status    = null,
        [Query] string? priority  = null,
        [Query] int     page      = 1,
        [Query] int     pageSize  = 20,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/commission/rules/{id}")]
    Task<CommissionRuleBffDto?> GetCommissionRuleByIdAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/commission/rules/stats")]
    Task<CommissionRuleStatsBffDto> GetCommissionRuleStatsAsync(
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/commission/rules")]
    Task<CommissionRuleCreateBffResult> CreateCommissionRuleAsync(
        [AizenRemoteCallBody] CreateCommissionRuleBffRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallPut("/api/v1/payment/commission/rules/{id}")]
    Task<CommissionRuleMutateBffResult> UpdateCommissionRuleAsync(
        long id,
        [AizenRemoteCallBody] UpdateCommissionRuleBffRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallDelete("/api/v1/payment/commission/rules/{id}")]
    Task<CommissionRuleMutateBffResult> DeactivateCommissionRuleAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/commission/rules/{id}/reactivate")]
    Task<CommissionRuleMutateBffResult> ReactivateCommissionRuleAsync(
        long id,
        CancellationToken ct = default);

    // ─── Invoices ─────────────────────────────────────────────────────────────

    [AizenRemoteCallPost("/api/v1/payment/invoices")]
    Task<CreateInvoiceDraftResult> CreateInvoiceDraftAsync(
        [AizenRemoteCallBody] CreateInvoiceDraftRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/invoices")]
    Task<InvoiceListResult> GetInvoicesPagedAsync(
        [Query] int    page     = 1,
        [Query] int    pageSize = 25,
        [Query] string? status   = null,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/invoices/{id}")]
    Task<InvoiceDto?> GetInvoiceAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/invoices/{id}/full")]
    Task<InvoiceDto?> GetInvoiceFullAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/invoices/{id}/issue")]
    Task<IssueInvoiceResult> IssueInvoiceAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallDelete("/api/v1/payment/invoices/{id}")]
    Task<CancelInvoiceResult> CancelInvoiceAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/invoices/buyer/{buyerId}")]
    Task<InvoiceListResult> GetInvoicesByBuyerAsync(
        long    buyerId,
        [Query] int page     = 1,
        [Query] int pageSize = 25,
        CancellationToken ct = default);

    // ─── Dashboard (gap report) ───────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/payment/admin/dashboard/kpis")]
    Task<PaymentDashboardKpisBffDto> GetDashboardKpisAsync(
        CancellationToken ct = default);

    // ─── Transaction Stats (gap report) ──────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/payment/admin/transactions/stats")]
    Task<PaymentTransactionStatsBffDto> GetTransactionStatsAsync(
        CancellationToken ct = default);

    // ─── Extended Payout management (gap report) ──────────────────────────────

    [AizenRemoteCallGet("/api/v1/payment/payouts")]
    Task<PaymentPayoutListBffResult> GetPayoutsPagedAsync(
        [Query] string? status   = null,
        [Query] int     page     = 1,
        [Query] int     pageSize = 25,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/payouts/{id}")]
    Task<PaymentPayoutBffDto?> GetPayoutDetailAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/payouts/stats")]
    Task<PaymentPayoutStatsBffDto> GetPayoutStatsAsync(
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/payouts/{id}/hold")]
    Task<MarkPayoutCompleteResult> HoldPayoutAsync(
        long id,
        [AizenRemoteCallBody] HoldPayoutRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/payouts/{id}/approve-manual")]
    Task<MarkPayoutCompleteResult> ApproveManualPayoutAsync(
        long id,
        [AizenRemoteCallBody] ApproveManualPayoutRequest body,
        CancellationToken ct = default);

    // ─── Subscription Stats (gap report) ─────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/payment/admin/subscriptions/stats")]
    Task<SubscriptionStatsBffDto> GetSubscriptionStatsAsync(
        CancellationToken ct = default);
}
