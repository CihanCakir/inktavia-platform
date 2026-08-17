using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Messaging.Abstraction.Request.Messaging;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.Messaging.Application.Command.MarkConversationRead;
using Aizen.Modules.Messaging.Application.Command.RequestAttachmentUploadUrl;
using Aizen.Modules.Messaging.Application.Command.SendMessage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Messaging.Controller.V1.Conversations;

[ApiController]
[Route("api/v1/conversations/{conversationId:long}/messages")]
[Tags("Messaging - Messages")]
[Authorize]
[DocumentationInfo("Conversation messages controller",
    "Handles sending and retrieving messages within a conversation.")]
public sealed class ConversationMessagesController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ConversationMessagesController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SendMessageResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SendMessageResponse?>> Send(
        [FromRoute] long conversationId,
        [FromBody] SendMessageRequest request,
        CancellationToken ct = default)
    {
        var command = new SendMessageCommand(
            conversationId, request.Content, request.Type, request.IsInternalNote,
            request.AttachmentFileStorageId, request.AttachmentFileName, request.AttachmentFileType,
            request.UploadSessionCode, request.Checksum, request.LocationJson,
            request.LocationLat, request.LocationLng, request.LocationLabel);
        var result = await _cqrs.ProcessAsync<SendMessageResponse>(command, ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Request a presigned S3 upload URL for a conversation attachment.
    /// Client uploads directly to S3, then calls SendMessage with UploadSessionCode.
    /// POST /api/v1/conversations/{conversationId}/messages/attachment-upload-url
    /// </summary>
    [HttpPost("attachment-upload-url")]
    [ProducesResponseType(typeof(RequestAttachmentUploadUrlResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RequestAttachmentUploadUrlResponse?>> GetAttachmentUploadUrl(
        [FromRoute] long conversationId,
        [FromBody] AttachmentUploadUrlRequest request,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<RequestAttachmentUploadUrlResponse>(
            new RequestAttachmentUploadUrlCommand
            {
                ConversationId = conversationId,
                FileName       = request.FileName,
                ContentType    = request.ContentType,
                SizeInBytes    = request.SizeInBytes
            }, ct);
        return SetResponse(result);
    }

    [HttpPatch("mark-read")]
    [Authorize(Roles = "Admin")]
    public async Task<AizenApiResponse<object?>> MarkRead(
        [FromRoute] long conversationId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<bool>(
            new MarkConversationReadCommand(conversationId), ct);
        return SetResponse<object>(result);
    }
}
