using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Messaging.Abstraction.Request.Messaging;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.Messaging.Application.Command.FlagConversation;
using Aizen.Modules.Messaging.Application.Command.ModerateMessage;
using Aizen.Modules.Messaging.Application.Query.GetModerationQueue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Messaging.Controller.V1.Moderation;

[ApiController]
[Route("api/v1/moderation")]
[Tags("Messaging - Moderation")]
[Authorize(Roles = "Admin")]
[DocumentationInfo("Conversation moderation controller",
    "Admin-only endpoints for content moderation: flag conversations, moderate messages, view moderation queue.")]
public sealed class ConversationModerationController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ConversationModerationController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet("queue")]
    [ProducesResponseType(typeof(GetModerationQueueResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetModerationQueueResponse?>> GetQueue(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetModerationQueueResponse>(
            new GetModerationQueueQuery(skip, take), ct);
        return SetResponse(result);
    }

    [HttpPatch("messages/{messageId:long}")]
    public async Task<AizenApiResponse<object?>> ModerateMessage(
        [FromRoute] long messageId,
        [FromBody] ModerateMessageRequest request,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<bool>(
            new ModerateMessageCommand(messageId, request.Status, request.Reason), ct);
        return SetResponse<object>(result);
    }

    [HttpPatch("conversations/{conversationId:long}/flag")]
    public async Task<AizenApiResponse<object?>> FlagConversation(
        [FromRoute] long conversationId,
        [FromBody] FlagConversationRequest request,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<bool>(
            new FlagConversationCommand(conversationId, request.Reason), ct);
        return SetResponse<object>(result);
    }
}
