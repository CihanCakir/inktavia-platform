using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Trip;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Trip;
using Aizen.Modules.ServiceRequest.Application.Command.Trip;
using Aizen.Modules.ServiceRequest.Application.Query.Owner;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ServiceRequest.Controller.V1.Trip;

/// <summary>
/// Live provider-trip tracking. The provider actions (start/location/arrive/cancel) are guarded to the ASSIGNED
/// provider on an accepted/active job (KeycloakTokenInfo.ProviderProfileId + assignment match). The trip read is
/// owner-scoped (UserInfo.UserId == SR.OwnerUserId). Both identities are asserted by the trusted BFFs.
/// </summary>
[ApiController]
[Route("api/v1/service-requests/{serviceRequestId:long}/trip")]
[Tags("ServiceRequest - Trip")]
[Authorize]
[DocumentationInfo("Trip endpoints", "Provider live-trip ingestion + owner trip read.")]
public sealed class ServiceRequestTripController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ServiceRequestTripController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpPost("start")]
    [ProducesResponseType(typeof(TripActionResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<TripActionResponse?>> Start([FromRoute] long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<TripActionResponse>(new StartTripCommand(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpPost("location")]
    [ProducesResponseType(typeof(TripActionResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<TripActionResponse?>> Location(
        [FromRoute] long serviceRequestId, [FromBody] TripLocationRequest req, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<TripActionResponse>(new PingTripCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }

    [HttpPost("arrive")]
    [ProducesResponseType(typeof(TripActionResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<TripActionResponse?>> Arrive([FromRoute] long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<TripActionResponse>(new ArriveTripCommand(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpPost("cancel")]
    [ProducesResponseType(typeof(TripActionResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<TripActionResponse?>> Cancel([FromRoute] long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<TripActionResponse>(new CancelTripCommand(serviceRequestId), ct);
        return SetResponse(result);
    }

    /// <summary>Owner-scoped current trip (null when there is none).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetServiceRequestTripForOwnerResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetServiceRequestTripForOwnerResponse?>> GetTrip([FromRoute] long serviceRequestId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<GetServiceRequestTripForOwnerResponse>(new GetServiceRequestTripForOwnerQuery(serviceRequestId), ct);
        return SetResponse(result);
    }
}
