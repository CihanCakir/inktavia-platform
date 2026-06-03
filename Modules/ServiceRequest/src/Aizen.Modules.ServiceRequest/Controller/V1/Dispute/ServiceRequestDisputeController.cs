using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;
using Aizen.Modules.ServiceRequest.Application.Command.Dispute;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Aizen.Modules.ServiceRequest.Controller.V1.Dispute;

[ApiController]
[Route("api/v1/service-requests/{serviceRequestId:long}/dispute")]
[Tags("ServiceRequest - Dispute")]
[Authorize]
[DocumentationInfo("Dispute endpoints", "Dispute lifecycle management for service requests.")]
public sealed class ServiceRequestDisputeController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ServiceRequestDisputeController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    private bool IsAdmin => ContextAccessor.HttpContext!.User.IsInRole("Admin");
    private bool IsProvider => ContextAccessor.HttpContext!.User.IsInRole("Provider");

    private ServiceRequestActorType ResolveActorType()
    {
        if (IsAdmin) return ServiceRequestActorType.Admin;
        if (IsProvider) return ServiceRequestActorType.Provider;
        return ServiceRequestActorType.Owner;
    }

    [HttpPost]
    [ProducesResponseType(typeof(OpenServiceRequestDisputeResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<OpenServiceRequestDisputeResponse?>> Open(
        [FromRoute] long serviceRequestId, [FromBody] OpenServiceRequestDisputeRequest req, CancellationToken ct = default)
    {
        var actorType = ResolveActorType();
        var result = await _cqrs.ProcessAsync<OpenServiceRequestDisputeResponse>(
            new OpenServiceRequestDisputeCommand(serviceRequestId, actorType, req), ct);
        return SetResponse(result);
    }

    [HttpPatch("{disputeId:long}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeStatus(
        [FromRoute] long serviceRequestId, [FromRoute] long disputeId,
        [FromBody] ChangeServiceRequestDisputeStatusRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<bool>(
            new ChangeServiceRequestDisputeStatusCommand(disputeId, req), ct);
        return Ok(new { Header = new { IsSuccess = true }, Body = new { Success = result } });
    }

    [HttpPatch("{disputeId:long}/resolve")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ResolveServiceRequestDisputeResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResolveServiceRequestDisputeResponse?>> Resolve(
        [FromRoute] long serviceRequestId, [FromRoute] long disputeId,
        [FromBody] ResolveServiceRequestDisputeRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ResolveServiceRequestDisputeResponse>(
            new ResolveServiceRequestDisputeCommand(disputeId, req), ct);
        return SetResponse(result);
    }
}
