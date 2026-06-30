using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Query.ServiceRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Aizen.Modules.ServiceRequest.Controller.V1.ServiceRequest;

[ApiController]
[Route("api/v1/service-requests")]
[Tags("ServiceRequest")]
[Authorize]
[DocumentationInfo("ServiceRequest endpoints", "Boat owner CRUD and lifecycle operations for service requests.")]
public sealed class ServiceRequestController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ServiceRequestController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    private long CurrentUserId =>
        long.Parse(ContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    [ProducesResponseType(typeof(CreateServiceRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CreateServiceRequestResponse?>> Create(
        [FromBody] CreateServiceRequestRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CreateServiceRequestResponse>(new CreateServiceRequestCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPut("{serviceRequestId:long}")]
    [ProducesResponseType(typeof(UpdateServiceRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateServiceRequestResponse?>> Update(
        [FromRoute] long serviceRequestId, [FromBody] UpdateServiceRequestRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<UpdateServiceRequestResponse>(new UpdateServiceRequestCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }

    [HttpGet("{serviceRequestId:long}")]
    [ProducesResponseType(typeof(GetServiceRequestDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetServiceRequestDetailResponse?>> GetDetail(
        [FromRoute] long serviceRequestId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetServiceRequestDetailResponse>(new GetServiceRequestDetailQuery(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(GetServiceRequestListResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetServiceRequestListResponse?>> GetMyList(
        [FromQuery] ServiceRequestListFilterRequest filter, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetServiceRequestListResponse>(new GetServiceRequestListQuery(CurrentUserId, filter), ct);
        return SetResponse(result);
    }

    [HttpPatch("{serviceRequestId:long}/cancel")]
    [ProducesResponseType(typeof(CancelServiceRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CancelServiceRequestResponse?>> Cancel(
        [FromRoute] long serviceRequestId, [FromBody] CancelServiceRequestRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CancelServiceRequestResponse>(new CancelServiceRequestCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }

    [HttpPatch("{serviceRequestId:long}/publish")]
    [ProducesResponseType(typeof(UpdateServiceRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateServiceRequestResponse?>> Publish(
        [FromRoute] long serviceRequestId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<UpdateServiceRequestResponse>(new PublishServiceRequestCommand(serviceRequestId), ct);
        return SetResponse(result);
    }

    [HttpPost("{serviceRequestId:long}/attachments")]
    [ProducesResponseType(typeof(AddServiceRequestAttachmentResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AddServiceRequestAttachmentResponse?>> AddAttachment(
        [FromRoute] long serviceRequestId, [FromBody] AddServiceRequestAttachmentRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<AddServiceRequestAttachmentResponse>(new AddServiceRequestAttachmentCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }
}
