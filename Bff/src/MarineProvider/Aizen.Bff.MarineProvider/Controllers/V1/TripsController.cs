using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.Trips;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Trip;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Trip;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

/// <summary>
/// Phase-2 live-trip publishing (browser v1 client). Provider-auth; the module guards to the ASSIGNED provider on an
/// accepted job. The publisher UI is a separate-repo follow-up — this is the ingestion API both the browser and a
/// future provider mobile app publish to.
/// </summary>
[ApiController]
[Route("api/v1/provider/trips")]
[Tags("Provider - Trips")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderActive)]
public sealed class TripsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public TripsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    /// <summary>Mark "en route" — opens live tracking for the accepted job.</summary>
    [HttpPost("{serviceRequestId:long}/start")]
    [ProducesResponseType(typeof(TripActionResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<TripActionResponse?>> Start(long serviceRequestId, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync(new StartTripBffCommand { ServiceRequestId = serviceRequestId }, ct));

    /// <summary>Publish a position ping (throttled &lt;3s server-side).</summary>
    [HttpPost("{serviceRequestId:long}/location")]
    [ProducesResponseType(typeof(TripActionResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<TripActionResponse?>> Location(
        long serviceRequestId, [FromBody] TripLocationRequest body, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync(new PingTripBffCommand { ServiceRequestId = serviceRequestId, Body = body }, ct));

    /// <summary>Mark "arrived" — closes tracking, stores the summary, purges the trail.</summary>
    [HttpPost("{serviceRequestId:long}/arrive")]
    [ProducesResponseType(typeof(TripActionResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<TripActionResponse?>> Arrive(long serviceRequestId, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync(new ArriveTripBffCommand { ServiceRequestId = serviceRequestId }, ct));

    /// <summary>Cancel the trip — closes tracking, stores the summary, purges the trail.</summary>
    [HttpPost("{serviceRequestId:long}/cancel")]
    [ProducesResponseType(typeof(TripActionResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<TripActionResponse?>> Cancel(long serviceRequestId, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync(new CancelTripBffCommand { ServiceRequestId = serviceRequestId }, ct));
}
