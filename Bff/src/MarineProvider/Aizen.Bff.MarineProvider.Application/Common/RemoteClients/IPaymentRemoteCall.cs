using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using Aizen.Modules.Payment.Abstraction.Request;

namespace Aizen.Bff.MarineProvider.Application.Common.RemoteClients;

public interface IPaymentRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/payment/provider/payouts")]
    Task<AizenApiResponse<ProviderPayoutPagedResultDto>> GetPayouts(
        [Refit.Query] int? status = null, [Refit.Query] int page = 1, [Refit.Query] int pageSize = 20);

    [AizenRemoteCallGet("/api/v1/payment/provider/payouts/summary")]
    Task<AizenApiResponse<ProviderPayoutSummaryDto>> GetPayoutSummary();

    [AizenRemoteCallGet("/api/v1/payment/provider/payment-profile")]
    Task<AizenApiResponse<ProviderPaymentProfileDto>> GetPaymentProfile();

    [AizenRemoteCallPut("/api/v1/payment/provider/payment-profile")]
    Task<AizenApiResponse<ProviderPaymentProfileDto>> UpsertPaymentProfile(
        [AizenRemoteCallBody] UpsertProviderPaymentProfileRequest body);

    [AizenRemoteCallGet("/api/v1/payment/provider/transactions")]
    Task<AizenApiResponse<ProviderTransactionPagedResultDto>> GetTransactions(
        [Refit.Query] int? status = null, [Refit.Query] int? type = null,
        [Refit.Query] int page = 1, [Refit.Query] int pageSize = 20,
        [Refit.Query] string? from = null, [Refit.Query] string? to = null);

    [AizenRemoteCallGet("/api/v1/payment/provider/invoices")]
    Task<AizenApiResponse<ProviderInvoicePagedResultDto>> GetInvoices(
        [Refit.Query] int? status = null, [Refit.Query] int? type = null,
        [Refit.Query] int page = 1, [Refit.Query] int pageSize = 20,
        [Refit.Query] string? from = null, [Refit.Query] string? to = null);

    [AizenRemoteCallGet("/api/v1/payment/provider/invoices/{id}")]
    Task<AizenApiResponse<ProviderInvoiceDetailDto>> GetInvoiceById(long id);

    [AizenRemoteCallGet("/api/v1/payment/provider/subscription")]
    Task<AizenApiResponse<ProviderSubscriptionDto>> GetSubscription();

    [AizenRemoteCallGet("/api/v1/payment/provider/plans")]
    Task<AizenApiResponse<List<ProviderPlanDto>>> GetPlans();

    [AizenRemoteCallGet("/api/v1/payment/provider/invoices/{id}/pdf-url")]
    Task<AizenApiResponse<ProviderFilePdfUrlDto>> GetInvoicePdfUrl(long id);

    [AizenRemoteCallGet("/api/v1/payment/provider/payouts/{id}/receipt-url")]
    Task<AizenApiResponse<ProviderFilePdfUrlDto>> GetPayoutReceiptUrl(long id);

    // ── BE-P11 premium offer boost (by-subject) ───────────────────────────────
    [AizenRemoteCallPost("/api/v1/payment/provider/offer-boost")]
    Task<AizenApiResponse<PurchaseOfferBoostResult>> PurchaseOfferBoost(
        [AizenRemoteCallBody] PurchaseOfferBoostRequest body);

    [AizenRemoteCallGet("/api/v1/payment/provider/offers/{offerId}/boost-status")]
    Task<AizenApiResponse<OfferBoostStatusDto>> GetOfferBoostStatus(long offerId);

    // ── BE-S7 / BE-S6 offer-builder economics preview (compute-on-demand, no persistence) ──
    // Internal Payment endpoints ([Authorize] service-to-service). The delegating handler injects the service token;
    // they return the RAW resolver DTO (not the AizenApiResponse envelope), like the ServiceRequest module's own calls.

    [AizenRemoteCallPost("/api/v1/payment/internal/commission/resolve-lines")]
    Task<ResolveLineCommissionsRemoteCallResponse> ResolveLineCommissions(
        [AizenRemoteCallBody] ResolveLineCommissionsRemoteCallRequest body);

    [AizenRemoteCallPost("/api/v1/payment/internal/discount/resolve-customer-discount")]
    Task<ResolveCustomerDiscountRemoteCallResponse> ResolveCustomerDiscount(
        [AizenRemoteCallBody] ResolveCustomerDiscountRemoteCallRequest body);
}
