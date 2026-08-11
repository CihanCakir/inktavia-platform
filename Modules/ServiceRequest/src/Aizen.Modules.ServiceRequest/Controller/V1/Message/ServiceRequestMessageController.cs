using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Message;
using Aizen.Modules.ServiceRequest.Application.Command.Message;
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
        // The BFF assertion flow sets SenderTypeOverride so the module uses the correct sender type
        // (the BFF's service account doesn't have Provider/Owner roles — role detection falls through).
        var senderType = req.SenderTypeOverride ?? ResolveSenderType();
        var result = await _cqrs.ProcessAsync<SendServiceRequestMessageResponse>(
            new SendServiceRequestMessageCommand(serviceRequestId, senderType, req), ct);
        return SetResponse(result);
    }

    // BE_WC3d — removed the SR chat READ (GET .../{srId}/messages): its last reader (the provider Request-detail page)
    // was repointed to the unified Messaging thread. sr.Messages now has NO chat reader. The POST Send above stays
    // (the SR write path is retained for WC2 flag-OFF reversibility until WC4).
    // BE_WC3c — the SR chat mark-read (PATCH .../messages/mark-read) was already removed here (no reader).
}
