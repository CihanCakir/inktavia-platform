using Aizen.Modules.CargoDry.Abstraction.RemoteCall.Requests;
using Aizen.Modules.CargoDry.Abstraction.RemoteCall.Responses;
using Aizen.Modules.CargoDry.Application.Commands.RecordCargoDrySupplySale;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySupplyAcceptContext;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySupplyProviderContext;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.CargoDry.Controllers;

/// <summary>
/// Internal (service-to-service) CargoDry endpoints for the ServiceRequest module's supply flow. [Authorize] (valid JWT),
/// NOT admin-restricted — called by ServiceRequest as inter-module orchestration. Must not be exposed via the public gateway.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/cargodry/internal")]
public sealed class CargoDryInternalController : ControllerBase
{
    private readonly ISender _sender;
    public CargoDryInternalController(ISender sender) => _sender = sender;

    /// <summary>
    /// Records the retail sale of an activated supply kit against its source CARGODRY_SUPPLY SR: enriches the
    /// kit-activation attribution (SalePrice + agreement commission → settlement roll-up) and records the owner's
    /// preferred provider on first completion. Idempotent per SR.
    /// </summary>
    [HttpPost("supply/record-sale")]
    public async Task<IActionResult> RecordSupplySale(
        [FromBody] RecordCargoDrySupplySaleRemoteRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new RecordCargoDrySupplySaleCommand
        {
            KitId            = request.KitId,
            ServiceRequestId = request.ServiceRequestId,
            OwnerUserId      = request.OwnerUserId,
            SaleAmount       = request.SaleAmount,
            CurrencyCode     = request.CurrencyCode,
            ResolvedByUserId = request.ResolvedByUserId,
        }, ct);

        return Ok(new RecordCargoDrySupplySaleRemoteResponse
        {
            Recorded             = result!.Recorded,
            AttributionId        = result.AttributionId,
            PreferredProviderSet = result.PreferredProviderSet,
            Note                 = result.Note,
        });
    }

    /// <summary>
    /// Server-authoritative accept context for a CARGODRY_SUPPLY request: product active/retail state + whether the
    /// provider holds an ACTIVE ConsignmentAgreement (the program-membership gate) for the product.
    /// </summary>
    [HttpGet("supply/accept-context")]
    public async Task<IActionResult> GetAcceptContext(
        [FromQuery] long providerProfileId, [FromQuery] string productCode, CancellationToken ct)
    {
        var result = await _sender.Send(
            new GetCargoDrySupplyAcceptContextQuery { ProviderProfileId = providerProfileId, ProductCode = productCode }, ct);

        return Ok(new GetCargoDrySupplyAcceptContextRemoteResponse
        {
            ProductActive              = result!.ProductActive,
            RetailPrice                = result.RetailPrice,
            CurrencyCode               = result.CurrencyCode,
            ProviderHasActiveAgreement = result.ProviderHasActiveAgreement,
            AvailableKitCount          = result.AvailableKitCount,
        });
    }

    /// <summary>
    /// Batch context for a provider's discovery page: acceptable product codes + owner ids preferring this provider.
    /// Lets the provider BFF compute canAccept + isPreferred for a page in one call.
    /// </summary>
    [HttpPost("supply/provider-context")]
    public async Task<IActionResult> GetProviderContext(
        [FromBody] GetCargoDrySupplyProviderContextRemoteRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new GetCargoDrySupplyProviderContextQuery
        {
            ProviderProfileId = request.ProviderProfileId,
            ProductCodes      = request.ProductCodes,
        }, ct);

        return Ok(new GetCargoDrySupplyProviderContextRemoteResponse
        {
            AcceptableProductCodes = result!.AcceptableProductCodes,
            PreferredOwnerUserIds  = result.PreferredOwnerUserIds,
            ProductStock           = result.ProductStock
                .Select(s => new CargoDrySupplyProductStockRemoteDto
                {
                    ProductCode = s.ProductCode, HasActiveAgreement = s.HasActiveAgreement, AvailableKitCount = s.AvailableKitCount,
                }).ToList(),
        });
    }
}
