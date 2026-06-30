using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Conversation;
using Aizen.Modules.ServiceRequest.Application.Query.Conversation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ServiceRequest.Controller.V1.Message;

[ApiController]
[Route("api/v1/messages")]
[Tags("ServiceRequest - Conversations")]
[Authorize]
[DocumentationInfo("ServiceRequest conversation endpoints", "Conversation thread management for service requests.")]
public sealed class ServiceRequestConversationController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ServiceRequestConversationController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet("conversations")]
    [ProducesResponseType(typeof(GetConversationListResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetConversationListResponse?>> GetList(
        [FromQuery] string? filter, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetConversationListResponse>(new GetConversationListQuery(filter), ct);
        return SetResponse(result);
    }

    [HttpGet("conversations/{conversationId:long}")]
    [ProducesResponseType(typeof(GetConversationDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetConversationDetailResponse?>> GetDetail(
        [FromRoute] long conversationId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetConversationDetailResponse>(new GetConversationDetailQuery(conversationId), ct);
        return SetResponse(result);
    }
}
