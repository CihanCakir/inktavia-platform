using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Commands.ActivateConsignmentAgreement;
using Aizen.Modules.CargoDry.Application.Commands.CreateConsignmentAgreement;
using Aizen.Modules.CargoDry.Application.Commands.SuspendConsignmentAgreement;
using Aizen.Modules.CargoDry.Application.Commands.TerminateConsignmentAgreement;
using Aizen.Modules.CargoDry.Application.Commands.UpdateConsignmentAgreement;
using Aizen.Modules.CargoDry.Application.Queries.GetActiveConsignmentAgreementForProvider;
using Aizen.Modules.CargoDry.Application.Queries.GetConsignmentAgreementByCode;
using Aizen.Modules.CargoDry.Application.Queries.GetConsignmentAgreementById;
using Aizen.Modules.CargoDry.Application.Queries.GetConsignmentAgreementsPaged;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.CargoDry.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/cargodry/admin/consignment/agreements")]
public sealed class CargoDryConsignmentAgreementsController : ControllerBase
{
    private readonly ISender _sender;

    public CargoDryConsignmentAgreementsController(ISender sender)
    {
        _sender = sender;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/cargodry/admin/consignment/agreements?page=1&amp;pageSize=25</summary>
    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] long?                       providerProfileId = null,
        [FromQuery] string?                     productCode       = null,
        [FromQuery] ConsignmentAgreementStatus? status            = null,
        [FromQuery] DateTime?                   dateFrom          = null,
        [FromQuery] DateTime?                   dateTo            = null,
        [FromQuery] string?                     search            = null,
        [FromQuery] int                         page              = 1,
        [FromQuery] int                         pageSize          = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetConsignmentAgreementsPagedQuery
        {
            ProviderProfileId = providerProfileId,
            ProductCode       = productCode,
            Status            = status,
            DateFrom          = dateFrom,
            DateTo            = dateTo,
            Search            = search,
            Page              = page,
            PageSize          = pageSize,
        }, ct);

        return Ok(result);
    }

    /// <summary>GET /api/v1/cargodry/admin/consignment/agreements/{id}</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _sender.Send(
            new GetConsignmentAgreementByIdQuery { Id = id }, ct);

        if (result is null) return NotFound();
        return Ok(result);
    }

    /// <summary>GET /api/v1/cargodry/admin/consignment/agreements/code/{code}</summary>
    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetByCode(string code, CancellationToken ct)
    {
        var result = await _sender.Send(
            new GetConsignmentAgreementByCodeQuery { AgreementCode = code }, ct);

        if (result is null) return NotFound();
        return Ok(result);
    }

    /// <summary>GET /api/v1/cargodry/admin/consignment/agreements/provider/{providerProfileId}/active?productCode=X</summary>
    [HttpGet("provider/{providerProfileId:long}/active")]
    public async Task<IActionResult> GetActiveForProvider(
        long providerProfileId,
        [FromQuery] string productCode,
        CancellationToken ct)
    {
        var result = await _sender.Send(new GetActiveConsignmentAgreementForProviderQuery
        {
            ProviderProfileId = providerProfileId,
            ProductCode       = productCode,
        }, ct);

        if (result is null) return NotFound();
        return Ok(result);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>POST /api/v1/cargodry/admin/consignment/agreements</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateConsignmentAgreementRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new CreateConsignmentAgreementCommand
        {
            AgreementCode           = request.AgreementCode,
            ProviderProfileId       = request.ProviderProfileId,
            ProductCode             = request.ProductCode,
            ConsignmentRate         = request.ConsignmentRate,
            MinimumSettlementAmount = request.MinimumSettlementAmount,
            CurrencyCode            = request.CurrencyCode,
            MaxKitCount             = request.MaxKitCount,
            StartDateUtc            = request.StartDateUtc,
            EndDateUtc              = request.EndDateUtc,
            TermsDocumentRef        = request.TermsDocumentRef,
            Notes                   = request.Notes,
        }, ct);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>PUT /api/v1/cargodry/admin/consignment/agreements/{id}</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateConsignmentAgreementRequest request,
        CancellationToken ct)
    {
        var result = await _sender.Send(new UpdateConsignmentAgreementCommand
        {
            Id                      = id,
            ConsignmentRate         = request.ConsignmentRate,
            MinimumSettlementAmount = request.MinimumSettlementAmount,
            CurrencyCode            = request.CurrencyCode,
            MaxKitCount             = request.MaxKitCount,
            StartDateUtc            = request.StartDateUtc,
            EndDateUtc              = request.EndDateUtc,
            TermsDocumentRef        = request.TermsDocumentRef,
            Notes                   = request.Notes,
        }, ct);

        return Ok(result);
    }

    /// <summary>POST /api/v1/cargodry/admin/consignment/agreements/{id}/activate</summary>
    [HttpPost("{id:long}/activate")]
    public async Task<IActionResult> Activate(long id, CancellationToken ct)
    {
        var result = await _sender.Send(
            new ActivateConsignmentAgreementCommand { Id = id }, ct);
        return Ok(result);
    }

    /// <summary>POST /api/v1/cargodry/admin/consignment/agreements/{id}/suspend</summary>
    [HttpPost("{id:long}/suspend")]
    public async Task<IActionResult> Suspend(
        long id, [FromBody] AgreementReasonRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new SuspendConsignmentAgreementCommand
        {
            Id     = id,
            Reason = request.Reason,
        }, ct);
        return Ok(result);
    }

    /// <summary>POST /api/v1/cargodry/admin/consignment/agreements/{id}/terminate</summary>
    [HttpPost("{id:long}/terminate")]
    public async Task<IActionResult> Terminate(
        long id, [FromBody] AgreementReasonRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new TerminateConsignmentAgreementCommand
        {
            Id     = id,
            Reason = request.Reason,
        }, ct);
        return Ok(result);
    }
}

// ─── Request models ──────────────────────────────────────────────────────────

public sealed class CreateConsignmentAgreementRequest
{
    public string    AgreementCode           { get; init; } = default!;
    public long      ProviderProfileId       { get; init; }
    public string    ProductCode             { get; init; } = default!;
    public decimal   ConsignmentRate         { get; init; }
    public decimal   MinimumSettlementAmount { get; init; }
    public string    CurrencyCode            { get; init; } = "TRY";
    public int       MaxKitCount             { get; init; }
    public DateTime  StartDateUtc            { get; init; }
    public DateTime? EndDateUtc              { get; init; }
    public string?   TermsDocumentRef        { get; init; }
    public string?   Notes                   { get; init; }
}

public sealed class UpdateConsignmentAgreementRequest
{
    public decimal   ConsignmentRate         { get; init; }
    public decimal   MinimumSettlementAmount { get; init; }
    public string    CurrencyCode            { get; init; } = "TRY";
    public int       MaxKitCount             { get; init; }
    public DateTime  StartDateUtc            { get; init; }
    public DateTime? EndDateUtc              { get; init; }
    public string?   TermsDocumentRef        { get; init; }
    public string?   Notes                   { get; init; }
}

public sealed class AgreementReasonRequest
{
    public string Reason { get; init; } = default!;
}
