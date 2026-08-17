using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.Messaging.Domain.Interface;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Aizen.Modules.Messaging.Repository.Mapping;

namespace Aizen.Modules.Messaging.Application.Query.GetConversationDetail;

[DocumentationInfo("Get conversation detail query handler",
    "Returns full conversation detail, filtering blocked and internal-note messages for non-admin callers. Enriches attachments with presigned read URLs.")]
public sealed class GetConversationDetailQueryHandler
    : AizenQueryHandler<GetConversationDetailQuery, GetConversationDetailResponse>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessagingFileStorageService _fileStorage;

    public GetConversationDetailQueryHandler(
        IConversationRepository conversationRepository,
        IMessagingFileStorageService fileStorage)
    {
        _conversationRepository = conversationRepository;
        _fileStorage            = fileStorage;
    }

    public override async Task<GetConversationDetailResponse> Handle(
        GetConversationDetailQuery request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdWithMessagesAsync(
            request.ConversationId, cancellationToken)
            ?? throw new InvalidOperationException($"Conversation {request.ConversationId} not found.");

        var filteredMessages = conversation.Messages
            .Where(m => request.IsAdmin || !m.IsInternalNote)
            .Where(m => request.IsAdmin || m.ModerationStatus != MessageModerationStatus.Blocked)
            .ToList();

        // Build messages with enriched attachment read URLs
        var messages = new List<ChatMessageDto>(filteredMessages.Count);
        foreach (var m in filteredMessages)
        {
            var dto = m.ToDto();

            if (m.Type == MessageType.MediaAttachment && m.Attachments.Any())
            {
                var enriched = new List<AttachmentDto>(m.Attachments.Count);
                foreach (var attachment in m.Attachments)
                {
                    string? readUrl = null;
                    if (!string.IsNullOrWhiteSpace(attachment.FileStorageId)
                        && Guid.TryParse(attachment.FileStorageId, out var fileGuid))
                    {
                        readUrl = await _fileStorage.GetReadUrlAsync(
                            fileGuid, TimeSpan.FromMinutes(30), cancellationToken);
                    }

                    enriched.Add(new AttachmentDto(
                        attachment.FileStorageId ?? string.Empty,
                        attachment.FileName,
                        attachment.FileType,
                        readUrl));
                }

                dto = dto with { Attachments = enriched };
            }

            messages.Add(dto);
        }

        var participants = conversation.Participants
            .Select(p => new ParticipantDto(p.UserId.ToString(), p.DisplayName, p.Role.ToString()))
            .ToList();

        var detail = new ConversationDetailDto
        {
            Id           = conversation.Id.ToString(),
            Title        = conversation.Title,
            ContextType  = conversation.ContextType.ToString(),
            ContextId    = conversation.ContextId.ToString(),
            Status       = conversation.Status.ToString(),
            Participants = participants,
            Messages     = messages,
        };

        return new GetConversationDetailResponse(detail);
    }
}
