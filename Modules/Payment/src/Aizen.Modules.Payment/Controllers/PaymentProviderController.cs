using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Request;
using Aizen.Modules.Payment.Application.Commands.UpsertProviderPaymentProfile;
using Aizen.Modules.Payment.Application.Queries.GetProviderPaymentProfile;
using Aizen.Modules.Payment.Application.Queries.GetProviderPayouts;
using Aizen.Modules.Payment.Application.Queries.GetProviderPayoutSummary;
using Aizen.Modules.Payment.Application.Queries.GetProviderTransactions;
using Aizen.Modules.Payment.Application.Queries.GetProviderInvoices;
using Aizen.Modules.Payment.Application.Queries.GetProviderInvoiceById;
using Aizen.Modules.Payment.Application.Queries.GetProviderInvoicePdfUrl;
using Aizen.Modules.Payment.Application.Queries.GetProviderPayoutReceiptUrl;
using Aizen.Modules.Payment.Application.Queries.GetProviderSubscription;
using Aizen.Modules.Payment.Application.Queries.GetProviderPlans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

[ApiController]
[Route("api/v1/payment/provider")]
[Tags("Payment - Provider")]
[Authorize]
public sealed class PaymentProviderController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    private readonly IAizenInfoAccessor _info;

    public PaymentProviderController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs, IAizenInfoAccessor info)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
        _info = info;
    }

    private long ResolveProviderProfileId()
    {
        var pid = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (pid <= 0) throw new AizenBusinessException("Provider identity could not be resolved.");
        return pid;
    }

    [HttpGet("payouts")]
    [ProducesResponseType(typeof(ProviderPayoutPagedResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderPayoutPagedResultDto?>> GetPayouts(
        [FromQuery] int? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<ProviderPayoutPagedResultDto>(
            new GetProviderPayoutsQuery { ProviderProfileId = pid, Status = status, Page = page, PageSize = pageSize }, ct);
        return SetResponse(result);
    }

    [HttpGet("payouts/summary")]
    [ProducesResponseType(typeof(ProviderPayoutSummaryDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderPayoutSummaryDto?>> GetPayoutSummary(CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<ProviderPayoutSummaryDto>(
            new GetProviderPayoutSummaryQuery { ProviderProfileId = pid }, ct);
        return SetResponse(result);
    }

    [HttpGet("transactions")]
    [ProducesResponseType(typeof(ProviderTransactionPagedResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderTransactionPagedResultDto?>> GetTransactions(
        [FromQuery] int? status, [FromQuery] int? type, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<ProviderTransactionPagedResultDto>(
            new GetProviderTransactionsQuery
            {
                ProviderProfileId = pid,
                Status = status,
                Type = type,
                From = from.HasValue ? DateTime.SpecifyKind(from.Value, DateTimeKind.Utc) : null,
                To   = to.HasValue   ? DateTime.SpecifyKind(to.Value,   DateTimeKind.Utc) : null,
                Page = page,
                PageSize = pageSize,
            }, ct);
        return SetResponse(result);
    }

    [HttpGet("payment-profile")]
    [ProducesResponseType(typeof(ProviderPaymentProfileDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderPaymentProfileDto?>> GetPaymentProfile(CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<ProviderPaymentProfileDto>(
            new GetProviderPaymentProfileQuery { ProviderProfileId = pid }, ct);
        return SetResponse(result);
    }

    [HttpGet("invoices")]
    [ProducesResponseType(typeof(ProviderInvoicePagedResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderInvoicePagedResultDto?>> GetInvoices(
        [FromQuery] int? status, [FromQuery] int? type, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<ProviderInvoicePagedResultDto>(
            new GetProviderInvoicesQuery
            {
                ProviderProfileId = pid,
                Status = status,
                Type = type,
                From = from.HasValue ? DateTime.SpecifyKind(from.Value, DateTimeKind.Utc) : null,
                To   = to.HasValue   ? DateTime.SpecifyKind(to.Value,   DateTimeKind.Utc) : null,
                Page = page,
                PageSize = pageSize,
            }, ct);
        return SetResponse(result);
    }

    [HttpGet("invoices/{id:long}")]
    [ProducesResponseType(typeof(ProviderInvoiceDetailDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderInvoiceDetailDto?>> GetInvoiceById(long id, CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<ProviderInvoiceDetailDto>(
            new GetProviderInvoiceByIdQuery { ProviderProfileId = pid, InvoiceId = id }, ct);
        return SetResponse(result);
    }

    [HttpGet("invoices/{id:long}/pdf-url")]
    [ProducesResponseType(typeof(ProviderFilePdfUrlDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderFilePdfUrlDto?>> GetInvoicePdfUrl(long id, CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<ProviderFilePdfUrlDto>(
            new GetProviderInvoicePdfUrlQuery { ProviderProfileId = pid, InvoiceId = id }, ct);
        return SetResponse(result);
    }

    [HttpGet("payouts/{id:long}/receipt-url")]
    [ProducesResponseType(typeof(ProviderFilePdfUrlDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderFilePdfUrlDto?>> GetPayoutReceiptUrl(long id, CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<ProviderFilePdfUrlDto>(
            new GetProviderPayoutReceiptUrlQuery { ProviderProfileId = pid, PayoutId = id }, ct);
        return SetResponse(result);
    }

    [HttpGet("subscription")]
    [ProducesResponseType(typeof(ProviderSubscriptionDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderSubscriptionDto?>> GetSubscription(CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<ProviderSubscriptionDto>(
            new GetProviderSubscriptionQuery { ProviderProfileId = pid }, ct);
        return SetResponse(result);
    }

    [HttpGet("plans")]
    [ProducesResponseType(typeof(List<ProviderPlanDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<ProviderPlanDto>?>> GetPlans(CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<List<ProviderPlanDto>>(
            new GetProviderPlansQuery { ProviderProfileId = pid }, ct);
        return SetResponse(result);
    }

    [HttpPut("payment-profile")]
    [ProducesResponseType(typeof(ProviderPaymentProfileDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderPaymentProfileDto?>> UpsertPaymentProfile(
        [FromBody] UpsertProviderPaymentProfileRequest body, CancellationToken ct = default)
    {
        var pid = ResolveProviderProfileId();
        var result = await _cqrs.ProcessAsync<ProviderPaymentProfileDto>(
            new UpsertProviderPaymentProfileCommand
            {
                ProviderProfileId = pid,
                Iban      = body.Iban,
                LegalName = body.LegalName,
                TaxNumber = body.TaxNumber,
            }, ct);
        return SetResponse(result);
    }
}
