using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Message;
using Aizen.Modules.ServiceRequest.Application.Command.Message;
using Aizen.Modules.ServiceRequest.Application.Query.Message;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Aizen.Modules.ServiceRequest.Controller.V1.Message;

[ApiController]
[Route("api/v1/service-requests/{serviceRequestId:long}/messages")]
[Tags("ServiceRequest - Messages")]
[Authorize]
[DocumentationInfo("Message endpoints", "Messaging within a service request thread.")]
public sealed class ServiceRequestMessageController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ServiceRequestMessageController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    private bool IsAdmin => ContextAccessor.HttpContext!.User.IsInRole("Admin");
    private bool IsProvider => ContextAccessor.HttpContext!.User.IsInRole("Provider");

    private ServiceRequestMessageSenderType ResolveSenderType()
    {
        if (IsAdmin) return ServiceRequestMessageSenderType.Admin;
        if (IsProvider) return ServiceRequestMessageSenderType.Provider;
        return ServiceRequestMessageSenderType.Owner;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SendServiceRequestMessageResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SendServiceRequestMessageResponse?>> Send(
        [FromRoute] long serviceRequestId, [FromBody] SendServiceRequestMessageRequest req, CancellationToken ct = default)
    {
        var senderType = ResolveSenderType();
        var result = await _cqrs.ProcessAsync<SendServiceRequestMessageResponse>(
            new SendServiceRequestMessageCommand(serviceRequestId, senderType, req), ct);
        return SetResponse(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetServiceRequestMessagesResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetServiceRequestMessagesResponse?>> GetMessages(
        [FromRoute] long serviceRequestId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetServiceRequestMessagesResponse>(
            new GetServiceRequestMessagesQuery(serviceRequestId, skip, take), ct);
        return SetResponse(result);
    }

    [HttpPatch("mark-read")]
    [ProducesResponseType(typeof(SendServiceRequestMessageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkRead(
        [FromRoute] long serviceRequestId, [FromBody] MarkServiceRequestMessagesReadRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<bool>(new MarkServiceRequestMessagesReadCommand(serviceRequestId, req), ct);
        return Ok(new { Header = new { IsSuccess = true }, Body = new { Success = result } });
    }
}
