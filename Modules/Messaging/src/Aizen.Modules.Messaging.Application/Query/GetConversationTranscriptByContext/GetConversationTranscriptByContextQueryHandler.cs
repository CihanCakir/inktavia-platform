using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.Messaging.Domain.Interface.Repository;

namespace Aizen.Modules.Messaging.Application.Query.GetConversationTranscriptByContext;

[DocumentationInfo("Get conversation transcript by context query handler",
    "BE_WC3b — returns the ordered chat transcript for a context (SR) for cross-module composition (dispute case).")]
public sealed class GetConversationTranscriptByContextQueryHandler
    : AizenQueryHandler<GetConversationTranscriptByContextQuery, ConversationTranscriptResponse>
{
    private readonly IConversationRepository _conversationRepository;

    public GetConversationTranscriptByContextQueryHandler(IConversationRepository conversationRepository)
        => _conversationRepository = conversationRepository;

    public override async Task<ConversationTranscriptResponse?> Handle(
        GetConversationTranscriptByContextQuery request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByContextWithMessagesAsync(
            request.ContextType, request.ContextId, cancellationToken);

        // No conversation yet for this context → empty transcript (the caller must not 500 on a chat-less SR).
        if (conversation is null)
            return new ConversationTranscriptResponse();

        // The complete chat transcript: exclude soft-deleted, admin internal notes, and blocked messages (the same
        // shape a participant sees). Ordered by send time so the dispute case renders the conversation in order.
        var messages = conversation.Messages
            .Where(m => !m.IsDeleted && !m.IsInternalNote && m.ModerationStatus != MessageModerationStatus.Blocked)
            .OrderBy(m => m.SentAt)
            .Select(m => new TranscriptMessageDto
            {
                MessageId               = m.Id,
                SenderUserId            = m.SenderUserId,
                SenderRole              = (int)m.SenderRole,
                MessageType             = (int)m.Type,
                Content                 = m.Content,
                AttachmentFileStorageId = m.Attachments
                    .Select(a => a.FileStorageId)
                    .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id)),
                LocationLat             = m.LocationLat,
                LocationLng             = m.LocationLng,
                LocationLabel           = m.LocationLabel,
                SentAt                  = m.SentAt,
            })
            .ToList();

        return new ConversationTranscriptResponse { Messages = messages };
    }
}
