using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Messaging.Domain.Interface;
using Aizen.Modules.Messaging.Domain.Interface.Repository;

namespace Aizen.Modules.Messaging.Application.Command.RequestAttachmentUploadUrl;

[DocumentationInfo("Request attachment upload URL command handler",
    "Verifies participant membership and creates a presigned S3 upload session via FileStorage message bus.")]
public sealed class RequestAttachmentUploadUrlCommandHandler
    : AizenCommandHandler<RequestAttachmentUploadUrlCommand, RequestAttachmentUploadUrlResponse>
{
    private readonly IConversationRepository _conversations;
    private readonly IMessagingFileStorageService _fileStorage;
    private readonly IAizenInfoAccessor _info;

    public RequestAttachmentUploadUrlCommandHandler(
        IConversationRepository conversations,
        IMessagingFileStorageService fileStorage,
        IAizenInfoAccessor info)
    {
        _conversations = conversations;
        _fileStorage   = fileStorage;
        _info          = info;
    }

    public override async Task<RequestAttachmentUploadUrlResponse> Handle(
        RequestAttachmentUploadUrlCommand command, CancellationToken ct)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;

        var conversation = await _conversations.GetByIdAsync(command.ConversationId, ct)
            ?? throw new InvalidOperationException($"Conversation {command.ConversationId} not found.");

        var isParticipant = conversation.Participants.Any(p => p.UserId == userId && p.IsActive);
        if (!isParticipant)
            throw new UnauthorizedAccessException("You are not a participant of this conversation.");

        var session = await _fileStorage.CreateUploadSessionAsync(
            command.FileName, command.ContentType, command.SizeInBytes, userId, ct);

        return new RequestAttachmentUploadUrlResponse(
            session.FileId,
            session.UploadSessionCode,
            session.UploadUrl,
            session.ExpiresAt);
    }
}
