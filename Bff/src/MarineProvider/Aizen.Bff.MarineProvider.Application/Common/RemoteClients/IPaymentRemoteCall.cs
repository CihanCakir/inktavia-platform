using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Payment.Abstraction.Dto;
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
}
