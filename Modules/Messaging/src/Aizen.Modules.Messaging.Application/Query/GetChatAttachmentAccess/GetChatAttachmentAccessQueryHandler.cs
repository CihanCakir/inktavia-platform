using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.Messaging.Domain.Interface.Repository;

namespace Aizen.Modules.Messaging.Application.Query.GetChatAttachmentAccess;

/// <summary>
/// BE_WC3a — resolves the conversation for the context (with messages + attachments), authorizes the authenticated
/// caller as a participant, and checks whether <c>FileId.ToString()</c> matches a <c>FileStorageId</c> on any of its
/// message attachments. Returns <c>Authorized = false</c> (never throws) on any miss — no conversation, non-participant,
/// or unknown fileId — so the BFF can cleanly fall back to the SR access-check for request/evidence attachments.
/// </summary>
public sealed class GetChatAttachmentAccessQueryHandler
    : AizenQueryHandler<GetChatAttachmentAccessQuery, ChatAttachmentAccessResponse>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IAizenInfoAccessor _info;

    public GetChatAttachmentAccessQueryHandler(
        IConversationRepository conversationRepository,
        IAizenInfoAccessor info)
    {
        _conversationRepository = conversationRepository;
        _info                   = info;
    }

    public override async Task<ChatAttachmentAccessResponse> Handle(
        GetChatAttachmentAccessQuery request, CancellationToken cancellationToken)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;

        var conversation = await _conversationRepository.GetByContextWithMessagesAsync(
            request.ContextType, request.ContextId, cancellationToken);

        // No conversation yet, or the caller is not a participant → not authorized (the BFF falls back to the SR check).
        if (conversation is null || !conversation.Participants.Any(p => p.UserId == userId))
            return new ChatAttachmentAccessResponse(false);

        // The fileId must match an attachment (by FileStorageId) on one of the conversation's messages. Both old synced
        // images (MapMessage sets FileStorageId = fileId.ToString()) and new native sends store the same reference.
        var fileRef = request.FileId.ToString();
        var authorized = conversation.Messages.Any(m =>
            !m.IsDeleted && m.Attachments.Any(a => a.FileStorageId == fileRef));

        return new ChatAttachmentAccessResponse(authorized);
    }
}
