using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest.FallbackCargoDrySupplyToCargo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ServiceRequest.Controller.V1.Internal;

/// <summary>
/// Internal (service-to-service) ServiceRequest endpoints. [Authorize] (any valid JWT), NOT admin-restricted — invoked
/// by trusted callers (e.g. the AdminPanel BFF's dev-only reconciliation tools). Must not be exposed via the public gateway.
/// </summary>
[ApiController]
[Route("api/v1/servicerequest/internal")]
[Tags("Internal - ServiceRequest")]
[Authorize]
[DocumentationInfo("Internal ServiceRequest endpoints", "Service-to-service ServiceRequest orchestration + dev/ops reconciliation.")]
public sealed class ServiceRequestInternalController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ServiceRequestInternalController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>
    /// POST /api/v1/servicerequest/internal/cargodry/force-accept-timeout
    /// Forces the CargoDry-supply accept-timeout fallback for one SR (→ AwaitingShipment / direct cargo sale) without
    /// waiting for the hourly sweep. Returns true when the fallback fired, false when the SR was no longer eligible
    /// (status already moved past the open window). Dev/ops E2E aid — the AdminPanel BFF passthrough is environment-gated.
    /// </summary>
    [HttpPost("cargodry/force-accept-timeout")]
    [ProducesResponseType(typeof(ForceCargoDryAcceptTimeoutResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ForceCargoDryAcceptTimeoutResponse?>> ForceCargoDryAcceptTimeout(
        [FromBody] ForceCargoDryAcceptTimeoutRequest request, CancellationToken ct = default)
    {
        var fellBack = await _cqrs.ProcessAsync<bool>(
            new FallbackCargoDrySupplyToCargoCommand { ServiceRequestId = request.ServiceRequestId }, ct);
        return SetResponse(new ForceCargoDryAcceptTimeoutResponse { FellBack = fellBack });
    }
}
