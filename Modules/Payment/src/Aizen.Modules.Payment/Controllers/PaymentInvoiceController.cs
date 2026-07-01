using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Request;
using Aizen.Modules.Payment.Application.Commands.CancelInvoice;
using Aizen.Modules.Payment.Application.Commands.CreateInvoiceDraft;
using Aizen.Modules.Payment.Application.Commands.IssueInvoice;
using Aizen.Modules.Payment.Application.Queries.GetInvoiceById;
using Aizen.Modules.Payment.Application.Queries.GetInvoicesByBuyer;
using Aizen.Modules.Payment.Application.Queries.GetInvoicesPaged;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

/// <summary>
/// Invoice management endpoints.
///
/// ── Endpoint map ─────────────────────────────────────────────────────────────
///  POST   /api/v1/payment/invoices                   → CreateDraft
///  POST   /api/v1/payment/invoices/{id}/issue        → Issue (assign number, transition to Issued)
///  DELETE /api/v1/payment/invoices/{id}              → Cancel Draft
///  GET    /api/v1/payment/invoices/{id}              → GetById (header only)
///  GET    /api/v1/payment/invoices/{id}/full         → GetById with line items + tax breakdowns
///  GET    /api/v1/payment/invoices                   → Paged list (admin)
///  GET    /api/v1/payment/invoices/buyer/{buyerId}   → Paged list scoped to buyer
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/payment/invoices")]
public sealed class PaymentInvoiceController : ControllerBase
{
    private readonly ISender _sender;

    public PaymentInvoiceController(ISender sender) => _sender = sender;

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a Draft invoice. Financial totals are computed from line items server-side.
    /// InvoiceNumber is NOT assigned until /issue is called.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateDraft(
        [FromBody] CreateInvoiceDraftRequest request,
        CancellationToken ct)
    {
        var result = await _sender.Send(new CreateInvoiceDraftCommand
        {
            InvoiceType              = request.InvoiceType,
            CommercialModel          = request.CommercialModel,
            SourceType               = request.SourceType,
            SourceId                 = request.SourceId,
            SellerName               = request.SellerName,
            SellerUserId             = request.SellerUserId,
            SellerTaxNumber          = request.SellerTaxNumber,
            SellerTaxOffice          = request.SellerTaxOffice,
            SellerAddress            = request.SellerAddress,
            BuyerUserId              = request.BuyerUserId,
            BuyerName                = request.BuyerName ?? "—",
            BuyerTaxNumber           = request.BuyerTaxNumber,
            BuyerTaxOffice           = request.BuyerTaxOffice,
            BuyerAddress             = request.BuyerAddress,
            Currency                 = request.Currency ?? "TRY",
            PaymentTransactionId     = request.PaymentTransactionId,
            PaymentReleaseId         = request.PaymentReleaseId,
            CommissionCalculationId  = request.CommissionCalculationId,
            ProviderPayoutId         = request.ProviderPayoutId,
            UserSubscriptionId       = request.UserSubscriptionId,
            OriginalInvoiceId        = request.OriginalInvoiceId,
            DueDateUtc               = request.DueDateUtc,
            Notes                    = request.Notes,
            Lines                    = (request.Lines ?? []).Select(l => new InvoiceLineDraftItem(
                l.LineNumber,
                l.LineType,
                l.Description,
                l.Quantity,
                l.UnitPrice,
                l.TaxRate,
                l.UnitCode,
                l.DiscountAmount,
                l.ProductCode,
                l.ServiceCategoryCode,
                l.SourceType,
                l.SourceId)).ToList(),
        }, ct);

        return Ok(result);
    }

    /// <summary>
    /// Issues a Draft invoice: assigns a formatted invoice number and transitions to Issued.
    /// Idempotent — safe to call twice.
    /// </summary>
    [HttpPost("{id:long}/issue")]
    public async Task<IActionResult> Issue(long id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _sender.Send(new IssueInvoiceCommand
        {
            InvoiceId      = id,
            IssuedByUserId = userId,
        }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Cancels a Draft invoice. Only Draft status invoices can be cancelled.
    /// Issued invoices require a CreditNote instead.
    /// Idempotent — safe to call twice.
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Cancel(
        long id,
        [FromQuery] string? reason,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _sender.Send(new CancelInvoiceCommand
        {
            InvoiceId         = id,
            CancelledByUserId = userId,
            Reason            = reason,
        }, ct);
        return Ok(result);
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Returns invoice header (no lines). Use /full for lines + tax breakdowns.</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetInvoiceByIdQuery
        {
            InvoiceId    = id,
            IncludeLines = false,
        }, ct);
        return Ok(result);
    }

    /// <summary>Returns invoice with all line items and tax breakdowns.</summary>
    [HttpGet("{id:long}/full")]
    public async Task<IActionResult> GetByIdFull(long id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetInvoiceByIdQuery
        {
            InvoiceId    = id,
            IncludeLines = true,
        }, ct);
        return Ok(result);
    }

    /// <summary>Admin paged invoice list with optional filters.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] InvoiceType?   type,
        [FromQuery] InvoiceStatus? status,
        [FromQuery] string?        prefix,
        [FromQuery] long?          buyerUserId,
        [FromQuery] DateTime?      fromDate,
        [FromQuery] DateTime?      toDate,
        [FromQuery] string?        search,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetInvoicesPagedQuery
        {
            Type        = type,
            Status      = status,
            Prefix      = prefix,
            BuyerUserId = buyerUserId,
            FromDate    = fromDate,
            ToDate      = toDate,
            Search      = search,
            Page        = page,
            PageSize    = pageSize,
        }, ct);
        return Ok(result);
    }

    /// <summary>Returns paged invoices for a specific buyer user.</summary>
    [HttpGet("buyer/{buyerId:long}")]
    public async Task<IActionResult> GetByBuyer(
        long buyerId,
        [FromQuery] InvoiceStatus? status,
        [FromQuery] InvoiceType?   type,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetInvoicesByBuyerQuery
        {
            BuyerUserId = buyerId,
            Status      = status,
            Type        = type,
            Page        = page,
            PageSize    = pageSize,
        }, ct);
        return Ok(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private long? GetCurrentUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst("userId");
        return claim is not null && long.TryParse(claim.Value, out var id) ? id : null;
    }
}
