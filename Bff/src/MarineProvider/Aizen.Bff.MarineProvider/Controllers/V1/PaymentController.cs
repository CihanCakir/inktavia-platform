using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.Payment;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

[ApiController]
[Route("api/v1/provider/payment")]
[Tags("Provider - Payment")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderActive)]
public sealed class PaymentController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public PaymentController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpGet("payouts")]
    [ProducesResponseType(typeof(ProviderPayoutPagedResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderPayoutPagedResultDto?>> GetPayouts(
        [FromQuery] int? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetProviderPayoutsBffQuery { Status = status, Page = page, PageSize = pageSize }, ct));

    [HttpGet("payouts/summary")]
    [ProducesResponseType(typeof(ProviderPayoutSummaryDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderPayoutSummaryDto?>> GetPayoutSummary(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetProviderPayoutSummaryBffQuery(), ct));

    [HttpGet("payment-profile")]
    [ProducesResponseType(typeof(ProviderPaymentProfileDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderPaymentProfileDto?>> GetPaymentProfile(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetProviderPaymentProfileBffQuery(), ct));

    [HttpPut("payment-profile")]
    [ProducesResponseType(typeof(ProviderPaymentProfileDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderPaymentProfileDto?>> UpsertPaymentProfile(
        [FromBody] UpsertProviderPaymentProfileRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new UpsertProviderPaymentProfileBffCommand
        {
            Iban      = body.Iban,
            LegalName = body.LegalName,
            TaxNumber = body.TaxNumber,
        }, ct));

    [HttpGet("transactions")]
    [ProducesResponseType(typeof(ProviderTransactionPagedResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderTransactionPagedResultDto?>> GetTransactions(
        [FromQuery] int? status, [FromQuery] int? type, [FromQuery] string? from, [FromQuery] string? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetProviderTransactionsBffQuery { Status = status, Type = type, From = from, To = to, Page = page, PageSize = pageSize }, ct));

    [HttpGet("invoices")]
    [ProducesResponseType(typeof(ProviderInvoicePagedResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderInvoicePagedResultDto?>> GetInvoices(
        [FromQuery] int? status, [FromQuery] int? type, [FromQuery] string? from, [FromQuery] string? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetProviderInvoicesBffQuery { Status = status, Type = type, From = from, To = to, Page = page, PageSize = pageSize }, ct));

    [HttpGet("invoices/{id:long}")]
    [ProducesResponseType(typeof(ProviderInvoiceDetailDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderInvoiceDetailDto?>> GetInvoiceById(long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetProviderInvoiceByIdBffQuery { InvoiceId = id }, ct));

    [HttpGet("subscription")]
    [ProducesResponseType(typeof(ProviderSubscriptionDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderSubscriptionDto?>> GetSubscription(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetProviderSubscriptionBffQuery(), ct));

    [HttpGet("plans")]
    [ProducesResponseType(typeof(List<ProviderPlanDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<ProviderPlanDto>?>> GetPlans(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetProviderPlansBffQuery(), ct));

    [HttpGet("invoices/{id:long}/pdf-url")]
    [ProducesResponseType(typeof(ProviderFilePdfUrlDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderFilePdfUrlDto?>> GetInvoicePdfUrl(long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetProviderInvoicePdfUrlBffQuery { InvoiceId = id }, ct));

    [HttpGet("payouts/{id:long}/receipt-url")]
    [ProducesResponseType(typeof(ProviderFilePdfUrlDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderFilePdfUrlDto?>> GetPayoutReceiptUrl(long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetProviderPayoutReceiptUrlBffQuery { PayoutId = id }, ct));
}
