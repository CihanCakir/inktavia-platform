using Aizen.Bff.AdminPanel.Application.Finance.Dto;
using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Request;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.Requests;
using Refit;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Admin payment BFF remote call",
    "Defines BFF-to-Payment module calls for transaction monitoring, payout management, " +
    "subscription oversight, plan reads, commission lookup, and invoice management. " +
    "Auth headers (Authorization service token + optional X-Aizen-Bff-Assertion) are injected automatically " +
    "by AdminPanelBffAuthDelegatingHandler.")]
public interface IPaymentRemoteCall : IAizenRemoteCall
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
        [Query] bool includeInactive = false,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/participant-plans")]
    Task<List<ParticipantPlanBffDto>> GetParticipantPlansAsync(
        [Query] bool includeInactive = false,
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
        [Query] string?   ruleType          = null,
        [Query] string?   status            = null,
        [Query] string?   priority          = null,
        [Query] int       page              = 1,
        [Query] int       pageSize          = 20,
        [Query] string?   contextType       = null,
        [Query] string?   commercialModel   = null,
        [Query] string?   productCode       = null,
        [Query] string?   salesChannel      = null,
        [Query] string?   search            = null,
        [Query] long?     providerProfileId = null,
        [Query] DateTime? effectiveOnUtc    = null,
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
        [Query] string? status     = null,
        [Query] int     page       = 1,
        [Query] int     pageSize   = 25,
        [Query] long?   providerId = null,
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

    [AizenRemoteCallGet("/api/v1/payment/admin/subscriptions")]
    Task<AdminSubscriptionListBffResult> GetAdminSubscriptionListAsync(
        [Query] string? audience = null,
        [Query] string? status   = null,
        [Query] int     page     = 1,
        [Query] int     pageSize = 25,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/admin/subscriptions/mrr-trend")]
    Task<SubscriptionMrrTrendBffResult> GetSubscriptionMrrTrendAsync(
        [Query] int months = 6,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/admin/subscriptions/churn-risk")]
    Task<SubscriptionChurnRiskBffDto> GetSubscriptionChurnRiskAsync(
        CancellationToken ct = default);

    // ─── Plans — CRUD ─────────────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/payment/provider-plans/{id}")]
    Task<ProviderPlanBffDto?> GetProviderPlanByIdAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/provider-plans")]
    Task<PlanMutateBffResult> CreateProviderPlanAsync(
        [AizenRemoteCallBody] CreateProviderPlanBffRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallPut("/api/v1/payment/provider-plans/{id}")]
    Task<PlanMutateBffResult> UpdateProviderPlanAsync(
        long id,
        [AizenRemoteCallBody] UpdateProviderPlanBffRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/participant-plans/{id}")]
    Task<ParticipantPlanBffDto?> GetParticipantPlanByIdAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/participant-plans")]
    Task<PlanMutateBffResult> CreateParticipantPlanAsync(
        [AizenRemoteCallBody] CreateParticipantPlanBffRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallPut("/api/v1/payment/participant-plans/{id}")]
    Task<PlanMutateBffResult> UpdateParticipantPlanAsync(
        long id,
        [AizenRemoteCallBody] UpdateParticipantPlanBffRequest body,
        CancellationToken ct = default);

    // ─── Plans — Activate / Deactivate ───────────────────────────────────────

    [AizenRemoteCallPost("/api/v1/payment/provider-plans/{id}/activate")]
    Task<PlanMutateBffResult> ActivateProviderPlanAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/provider-plans/{id}/deactivate")]
    Task<PlanMutateBffResult> DeactivateProviderPlanAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/participant-plans/{id}/activate")]
    Task<PlanMutateBffResult> ActivateParticipantPlanAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/participant-plans/{id}/deactivate")]
    Task<PlanMutateBffResult> DeactivateParticipantPlanAsync(
        long id,
        CancellationToken ct = default);

    // ─── Finance Reporting (Phase 15) ────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/payment/finance/reports/invoice-statement")]
    Task<FinanceInvoiceStatementReportDto> GetFinanceInvoiceStatementAsync(
        [Query] InvoiceType?       type          = null,
        [Query] InvoiceStatus?     status        = null,
        [Query] InvoiceSourceType? sourceType    = null,
        [Query] long?              buyerUserId   = null,
        [Query] string?            currency      = null,
        [Query] DateTime?          fromDate      = null,
        [Query] DateTime?          toDate        = null,
        [Query] string?            search        = null,
        [Query] bool?              hasMismatches = null,
        [Query] int                page          = 1,
        [Query] int                pageSize      = 50,
        CancellationToken ct = default);

    // ── Financial reporting: §15 summary + ledger drill-down (BE-P12) ─────────

    [AizenRemoteCallGet("/api/v1/payment/finance/reports/financial-summary")]
    Task<FinancialSummaryReportBffDto> GetFinancialSummaryReportAsync(
        [Query] DateTime from,
        [Query] DateTime to,
        [Query] string   currency = "TRY",
        CancellationToken ct = default);

    // C1 — dashboard revenue chart: last `months` months of revenue + commission, oldest→newest, zero-filled.
    [AizenRemoteCallGet("/api/v1/payment/finance/reports/monthly-revenue-commission")]
    Task<List<Dashboard.Dto.MonthlyRevenueCommissionDto>> GetMonthlyRevenueCommissionReportAsync(
        [Query] int months = 12,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/finance/reports/ledger-entries")]
    Task<LedgerEntriesPageBffDto> GetLedgerEntriesAsync(
        [Query] LedgerAccountLine? accountLine       = null,
        [Query] LedgerSourceType?  sourceType        = null,
        [Query] long?              providerProfileId = null,
        [Query] DateTime?          from              = null,
        [Query] DateTime?          to                = null,
        [Query] string?            currency          = null,
        [Query] int                page              = 1,
        [Query] int                pageSize          = 50,
        CancellationToken ct = default);

    // ── Finance CSV Export (Phase 16G) ────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/payment/finance/reports/invoice-statement/export")]
    Task<HttpResponseMessage> ExportFinanceInvoiceStatementAsync(
        [Query] InvoiceType?       type          = null,
        [Query] InvoiceStatus?     status        = null,
        [Query] InvoiceSourceType? sourceType    = null,
        [Query] long?              buyerUserId   = null,
        [Query] string?            currency      = null,
        [Query] DateTime?          fromDate      = null,
        [Query] DateTime?          toDate        = null,
        [Query] string?            search        = null,
        [Query] bool?              hasMismatches = null,
        CancellationToken ct = default);

    // ─── Provider sub-merchant onboarding KYC review (BE-I1) ──────────────────

    [AizenRemoteCallGet("/api/v1/payment/admin/providers/sub-merchant/onboarding-queue")]
    Task<ProviderSubMerchantOnboardingQueueDto> GetSubMerchantOnboardingQueueAsync(
        [Query] ProviderSubMerchantOnboardingStatus? status = null,
        [Query] int page = 1,
        [Query] int pageSize = 20,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/admin/providers/{providerProfileId}/sub-merchant/verify")]
    Task<ProviderSubMerchantOnboardingResult> VerifySubMerchantAsync(
        long providerProfileId,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/admin/providers/{providerProfileId}/sub-merchant/reject")]
    Task<ProviderSubMerchantOnboardingResult> RejectSubMerchantAsync(
        long providerProfileId,
        [AizenRemoteCallBody] RejectSubMerchantBffRequest body,
        CancellationToken ct = default);

    // ─── BE-P3 PlatformFeeRule CRUD (module: /api/v1/payment/platform-fee) ────

    [AizenRemoteCallGet("/api/v1/payment/platform-fee/resolve")]
    Task<PlatformFeeResolveBffResult?> ResolvePlatformFeeAsync(
        [Query] string   currencyCode                 = "TRY",
        [Query] string?  categoryCode                 = null,
        [Query] string?  customerType                 = null,
        [Query] decimal  customerPayableServiceAmount = 0m,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/platform-fee/rules")]
    Task<PlatformFeeRuleCreateBffResult> CreatePlatformFeeRuleAsync(
        [AizenRemoteCallBody] CreatePlatformFeeRuleBffRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallPut("/api/v1/payment/platform-fee/rules/{id}")]
    Task<PlatformFeeRuleMutateBffResult> UpdatePlatformFeeRuleAsync(
        long id,
        [AizenRemoteCallBody] UpdatePlatformFeeRuleBffRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/platform-fee/rules/{id}/deactivate")]
    Task<PlatformFeeRuleMutateBffResult> DeactivatePlatformFeeRuleAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/platform-fee/rules")]
    Task<PlatformFeeRulesListBffResult> ListPlatformFeeRulesAsync(
        [Query] string?  model        = null,
        [Query] string?  status       = null,
        [Query] string?  currencyCode = null,
        [Query] string?  categoryCode = null,
        [Query] string?  customerType = null,
        [Query] int      page         = 1,
        [Query] int      pageSize     = 20,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/platform-fee/rules/stats")]
    Task<PlatformFeeRuleStatsBffDto> GetPlatformFeeRuleStatsAsync(
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/platform-fee/rules/{id}")]
    Task<PlatformFeeRuleDetailBffDto?> GetPlatformFeeRuleDetailAsync(
        long id,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/platform-fee/rules/{id}/reactivate")]
    Task<PlatformFeeRuleMutateBffResult> ReactivatePlatformFeeRuleAsync(
        long id,
        CancellationToken ct = default);

    // ─── BE-P4 ProviderPlanPrice CRUD (module: /api/v1/payment/plan-prices) ───

    [AizenRemoteCallGet("/api/v1/payment/plan-prices/plan/{planId}")]
    Task<List<ProviderPlanPriceBffDto>> GetProviderPlanPricesForPlanAsync(long planId, CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/plan-prices/resolve")]
    Task<ProviderPlanPriceBffDto?> ResolveProviderPlanPriceAsync(
        [Query] long      providerPlanId,
        [Query] string    currencyCode  = "TRY",
        [Query] string    billingPeriod = "Monthly",
        [Query] DateTime? atUtc         = null,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/plan-prices/upcoming-changes")]
    Task<List<UpcomingPriceChangeBffItem>> GetUpcomingPlanPriceChangesAsync(
        [Query] int withinDays = 14,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/plan-prices")]
    Task<ProviderPlanPriceCreateBffResult> CreateProviderPlanPriceAsync(
        [AizenRemoteCallBody] CreateProviderPlanPriceBffRequest body, CancellationToken ct = default);

    [AizenRemoteCallPut("/api/v1/payment/plan-prices/{id}")]
    Task<ProviderPlanPriceMutateBffResult> UpdateProviderPlanPriceAsync(
        long id, [AizenRemoteCallBody] UpdateProviderPlanPriceBffRequest body, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/plan-prices/{id}/deactivate")]
    Task<ProviderPlanPriceMutateBffResult> DeactivateProviderPlanPriceAsync(long id, CancellationToken ct = default);

    // ─── BE-P5 ProfitProtectionPolicy CRUD (module: /api/v1/payment/profit-protection) ──

    [AizenRemoteCallGet("/api/v1/payment/profit-protection/resolve")]
    Task<ProfitProtectionPolicyResolveBffResult?> ResolveProfitProtectionPolicyAsync(
        [Query] string currencyCode = "TRY",
        [Query] DateTime? atUtc = null,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/profit-protection/policies")]
    Task<ProfitProtectionPolicyCreateBffResult> CreateProfitProtectionPolicyAsync(
        [AizenRemoteCallBody] CreateProfitProtectionPolicyBffRequest body, CancellationToken ct = default);

    [AizenRemoteCallPut("/api/v1/payment/profit-protection/policies/{id}")]
    Task<ProfitProtectionPolicyMutateBffResult> UpdateProfitProtectionPolicyAsync(
        long id, [AizenRemoteCallBody] UpdateProfitProtectionPolicyBffRequest body, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/profit-protection/policies/{id}/deactivate")]
    Task<ProfitProtectionPolicyMutateBffResult> DeactivateProfitProtectionPolicyAsync(long id, CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/profit-protection/policies")]
    Task<ProfitProtectionPolicyListBffResult> ListProfitProtectionPoliciesAsync(
        [Query] string? currencyCode = null,
        [Query] string? status       = null,
        [Query] bool?   isActive     = null,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/profit-protection/policies/{id}")]
    Task<ProfitProtectionPolicyDetailBffDto?> GetProfitProtectionPolicyDetailAsync(long id, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/profit-protection/policies/{id}/reactivate")]
    Task<ProfitProtectionPolicyMutateBffResult> ReactivateProfitProtectionPolicyAsync(long id, CancellationToken ct = default);

    // ─── BE-P6 CustomerDiscountRule CRUD (module: /api/v1/payment/customer-discounts) ──

    [AizenRemoteCallGet("/api/v1/payment/customer-discounts/resolve")]
    Task<CustomerDiscountResolveBffResult?> ResolveCustomerDiscountAsync(
        [Query] long?    customerPlanId                = null,
        [Query] string?  categoryCode                  = null,
        [Query] string   currencyCode                  = "TRY",
        [Query] decimal  serviceBaseAmount             = 0m,
        [Query] bool     providerConsent               = false,
        [Query] long?    participantPlanSubscriptionId = null,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/customer-discounts/rules")]
    Task<CustomerDiscountRuleCreateBffResult> CreateCustomerDiscountRuleAsync(
        [AizenRemoteCallBody] CreateCustomerDiscountRuleBffRequest body, CancellationToken ct = default);

    [AizenRemoteCallPut("/api/v1/payment/customer-discounts/rules/{id}")]
    Task<CustomerDiscountRuleMutateBffResult> UpdateCustomerDiscountRuleAsync(
        long id, [AizenRemoteCallBody] UpdateCustomerDiscountRuleBffRequest body, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/customer-discounts/rules/{id}/deactivate")]
    Task<CustomerDiscountRuleMutateBffResult> DeactivateCustomerDiscountRuleAsync(long id, CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/customer-discounts/rules")]
    Task<CustomerDiscountRuleListBffResult> ListCustomerDiscountRulesAsync(
        [Query] long?   customerPlanId = null,
        [Query] string? categoryCode   = null,
        [Query] string? currencyCode   = null,
        [Query] string? fundingMode    = null,
        [Query] bool?   isActive       = null,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/customer-discounts/rules/{id}")]
    Task<CustomerDiscountRuleDetailBffDto?> GetCustomerDiscountRuleDetailAsync(long id, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/customer-discounts/rules/{id}/reactivate")]
    Task<CustomerDiscountRuleMutateBffResult> ReactivateCustomerDiscountRuleAsync(long id, CancellationToken ct = default);

    // ─── BE-P6 CustomerBenefitBudgetPolicy (module: /api/v1/payment/benefit-budget) ──

    [AizenRemoteCallPost("/api/v1/payment/benefit-budget/policies")]
    Task<CustomerBenefitBudgetPolicyCreateBffResult> CreateCustomerBenefitBudgetPolicyAsync(
        [AizenRemoteCallBody] CreateCustomerBenefitBudgetPolicyBffRequest body, CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/benefit-budget/policies")]
    Task<CustomerBenefitBudgetPolicyListBffResult> ListCustomerBenefitBudgetPoliciesAsync(
        [Query] long?   customerPlanId = null,
        [Query] string? currencyCode   = null,
        [Query] bool?   isActive       = null,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/benefit-budget/policies/{id}")]
    Task<CustomerBenefitBudgetPolicyDetailBffDto?> GetCustomerBenefitBudgetPolicyDetailAsync(long id, CancellationToken ct = default);

    // ─── BE-S5 PartCommercialTerm (module: /api/v1/payment/part-commercial-term) — ADMIN-ONLY, cost-bearing ──

    [AizenRemoteCallGet("/api/v1/payment/part-commercial-term/rules")]
    Task<PartCommercialTermListBffResult> ListPartCommercialTermsAsync(
        [Query] string? brand             = null,
        [Query] string? productCode       = null,
        [Query] long?   providerProfileId = null,
        [Query] string? categoryCode      = null,
        [Query] string? currencyCode      = null,
        [Query] bool?   isActive          = null,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/part-commercial-term/rules/{id}")]
    Task<PartCommercialTermBffDto?> GetPartCommercialTermDetailAsync(long id, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/part-commercial-term/rules")]
    Task<PartCommercialTermCreateBffResult> CreatePartCommercialTermAsync(
        [AizenRemoteCallBody] CreatePartCommercialTermBffRequest body, CancellationToken ct = default);

    [AizenRemoteCallPut("/api/v1/payment/part-commercial-term/rules/{id}")]
    Task<PartCommercialTermUpdateBffResult> UpdatePartCommercialTermAsync(
        long id, [AizenRemoteCallBody] UpdatePartCommercialTermBffRequest body, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/part-commercial-term/rules/{id}/deactivate")]
    Task<bool> DeactivatePartCommercialTermAsync(long id, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/part-commercial-term/rules/{id}/reactivate")]
    Task<bool> ReactivatePartCommercialTermAsync(long id, CancellationToken ct = default);

    // ─── BE-P7 ProviderCommissionBenefitRule + entitlement (module: /api/v1/payment/commission-benefits) ──

    [AizenRemoteCallGet("/api/v1/payment/commission-benefits/resolve")]
    Task<EffectiveCommissionResolveBffResult?> ResolveEffectiveCommissionAsync(
        [Query] long     providerProfileId,
        [Query] long?    providerPlanId       = null,
        [Query] string?  categoryCode         = null,
        [Query] decimal  serviceAmount        = 0m,
        [Query] string   currencyCode         = "TRY",
        [Query] decimal? eligibleGmvRemaining = null,
        [Query] decimal  planFloorRate        = 0m,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/commission-benefits/rules")]
    Task<ProviderCommissionBenefitRuleCreateBffResult> CreateProviderCommissionBenefitRuleAsync(
        [AizenRemoteCallBody] CreateProviderCommissionBenefitRuleBffRequest body, CancellationToken ct = default);

    [AizenRemoteCallPut("/api/v1/payment/commission-benefits/rules/{id}")]
    Task<ProviderCommissionBenefitRuleMutateBffResult> UpdateProviderCommissionBenefitRuleAsync(
        long id, [AizenRemoteCallBody] UpdateProviderCommissionBenefitRuleBffRequest body, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/commission-benefits/rules/{id}/deactivate")]
    Task<ProviderCommissionBenefitRuleMutateBffResult> DeactivateProviderCommissionBenefitRuleAsync(long id, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/commission-benefits/entitlements")]
    Task<GrantEntitlementBffResult> GrantProviderCommissionBenefitEntitlementAsync(
        [AizenRemoteCallBody] GrantProviderCommissionBenefitEntitlementBffRequest body, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/commission-benefits/entitlements/{id}/revoke")]
    Task<RevokeEntitlementBffResult> RevokeProviderCommissionBenefitEntitlementAsync(long id, CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/commission-benefits/rules")]
    Task<ProviderCommissionBenefitRuleListBffResult> ListProviderCommissionBenefitRulesAsync(
        [Query] long?   providerProfileId = null,
        [Query] long?   providerPlanId    = null,
        [Query] string? categoryCode      = null,
        [Query] string? currencyCode      = null,
        [Query] bool?   stackable         = null,
        [Query] bool?   isActive          = null,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/commission-benefits/rules/{id}")]
    Task<ProviderCommissionBenefitRuleDetailBffDto?> GetProviderCommissionBenefitRuleDetailAsync(long id, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/commission-benefits/rules/{id}/reactivate")]
    Task<ProviderCommissionBenefitRuleMutateBffResult> ReactivateProviderCommissionBenefitRuleAsync(long id, CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/commission-benefits/entitlements")]
    Task<ProviderCommissionBenefitEntitlementListBffResult> ListProviderCommissionBenefitEntitlementsAsync(
        [Query] long? providerProfileId = null,
        [Query] long? benefitRuleId     = null,
        [Query] bool? isActive          = null,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/commission-benefits/entitlements/{id}")]
    Task<ProviderCommissionBenefitEntitlementDetailBffDto?> GetProviderCommissionBenefitEntitlementDetailAsync(long id, CancellationToken ct = default);

    // ─── P11 Premium admin (module: /api/v1/payment/admin/premium) ────────────

    [AizenRemoteCallGet("/api/v1/payment/admin/premium/products")]
    Task<List<PremiumProductAdminDto>> GetPremiumProductsAsync(CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/admin/premium/products/{id}")]
    Task<PremiumProductAdminDto?> GetPremiumProductByIdAsync(long id, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/admin/premium/products")]
    Task<PremiumMutateResultDto> CreatePremiumProductAsync(
        [AizenRemoteCallBody] CreatePremiumProductBffRequest body, CancellationToken ct = default);

    [AizenRemoteCallPut("/api/v1/payment/admin/premium/products/{id}")]
    Task<PremiumMutateResultDto> UpdatePremiumProductAsync(
        long id, [AizenRemoteCallBody] UpdatePremiumProductBffRequest body, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/admin/premium/products/{id}/activate")]
    Task<PremiumMutateResultDto> ActivatePremiumProductAsync(long id, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/admin/premium/products/{id}/deactivate")]
    Task<PremiumMutateResultDto> DeactivatePremiumProductAsync(long id, CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/admin/premium/products/{productId}/prices")]
    Task<List<PremiumProductPriceAdminDto>> GetPremiumProductPricesAsync(long productId, CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/admin/premium/products/{productId}/prices/resolve")]
    Task<PremiumProductPriceAdminDto?> ResolvePremiumProductPriceAsync(
        long productId,
        [Query] string    currencyCode = "TRY",
        [Query] DateTime? atUtc        = null,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/admin/premium/prices")]
    Task<PremiumMutateResultDto> CreatePremiumProductPriceAsync(
        [AizenRemoteCallBody] CreatePremiumProductPriceBffRequest body, CancellationToken ct = default);

    [AizenRemoteCallPut("/api/v1/payment/admin/premium/prices/{id}")]
    Task<PremiumMutateResultDto> UpdatePremiumProductPriceAsync(
        long id, [AizenRemoteCallBody] UpdatePremiumProductPriceBffRequest body, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/admin/premium/prices/{id}/deactivate")]
    Task<PremiumMutateResultDto> DeactivatePremiumProductPriceAsync(long id, CancellationToken ct = default);

    // ─── P10 RefundAllocationPolicy (module: /api/v1/payment/admin/refund-allocation-policies) ──

    [AizenRemoteCallGet("/api/v1/payment/admin/refund-allocation-policies")]
    Task<List<RefundAllocationPolicyAdminDto>> GetRefundAllocationPoliciesAsync(CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/admin/refund-allocation-policies/resolve")]
    Task<RefundAllocationPolicyAdminDto?> ResolveRefundAllocationPolicyAsync(
        [Query] string    currencyCode = "TRY",
        [Query] DateTime? atUtc        = null,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/admin/refund-allocation-policies/{id}")]
    Task<RefundAllocationPolicyAdminDto?> GetRefundAllocationPolicyByIdAsync(long id, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/admin/refund-allocation-policies")]
    Task<RefundAllocationPolicyMutateResultDto> CreateRefundAllocationPolicyAsync(
        [AizenRemoteCallBody] CreateRefundAllocationPolicyBffRequest body, CancellationToken ct = default);

    [AizenRemoteCallPut("/api/v1/payment/admin/refund-allocation-policies/{id}")]
    Task<RefundAllocationPolicyMutateResultDto> UpdateRefundAllocationPolicyAsync(
        long id, [AizenRemoteCallBody] UpdateRefundAllocationPolicyBffRequest body, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/admin/refund-allocation-policies/{id}/deactivate")]
    Task<RefundAllocationPolicyMutateResultDto> DeactivateRefundAllocationPolicyAsync(long id, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/admin/refund-allocation-policies/{id}/reactivate")]
    Task<RefundAllocationPolicyMutateResultDto> ReactivateRefundAllocationPolicyAsync(long id, CancellationToken ct = default);

    // ─── P10 Refund / Chargeback queues (module: /api/v1/payment/admin) ───────

    [AizenRemoteCallGet("/api/v1/payment/admin/refund-queue")]
    Task<RefundQueuePagedDto> GetRefundQueueAsync(
        [Query] RefundCause?             cause        = null,
        [Query] ReleaseState?            releaseState = null,
        [Query] TransactionRefundStatus? status       = null,
        [Query] int                      page         = 1,
        [Query] int                      pageSize     = 20,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/admin/chargeback-queue")]
    Task<ChargebackQueuePagedDto> GetChargebackQueueAsync(
        [Query] int page     = 1,
        [Query] int pageSize = 20,
        CancellationToken ct = default);

    // ─── P10 ProviderBalance ledger (module: /api/v1/payment/admin/provider-balances) ──

    [AizenRemoteCallGet("/api/v1/payment/admin/provider-balances")]
    Task<ProviderBalancePagedDto> GetProviderBalancesAsync(
        [Query] string? currency      = null,
        [Query] bool    onlyNegative  = false,
        [Query] int     page          = 1,
        [Query] int     pageSize      = 20,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/payment/admin/provider-balances/{providerProfileId}")]
    Task<ProviderBalanceAdminDto?> GetProviderBalanceAsync(
        long providerProfileId,
        [Query] string currency = "TRY",
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/payment/admin/provider-balances/{providerProfileId}/adjust")]
    Task<ProviderBalanceAdjustResultDto> AdjustProviderBalanceAsync(
        long providerProfileId,
        [AizenRemoteCallBody] AdjustProviderBalanceBffRequest body,
        CancellationToken ct = default);
}
