using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Aizen.Modules.Messaging.Repository.Mapping;

namespace Aizen.Modules.Messaging.Application.Query.GetConversationDetail;

[DocumentationInfo("Get conversation detail query handler",
    "Returns full conversation detail, filtering blocked and internal-note messages for non-admin callers.")]
public sealed class GetConversationDetailQueryHandler
    : AizenQueryHandler<GetConversationDetailQuery, GetConversationDetailResponse>
{
    private readonly IConversationRepository _conversationRepository;

    public GetConversationDetailQueryHandler(IConversationRepository conversationRepository)
        => _conversationRepository = conversationRepository;

    public override async Task<GetConversationDetailResponse> Handle(
        GetConversationDetailQuery request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdWithMessagesAsync(
            request.ConversationId, cancellationToken)
            ?? throw new InvalidOperationException($"Conversation {request.ConversationId} not found.");

        var messages = conversation.Messages
            .Where(m => request.IsAdmin || !m.IsInternalNote)
            .Where(m => request.IsAdmin || m.ModerationStatus != MessageModerationStatus.Blocked)
            .Select(m => m.ToDto())
            .ToList();

        var participants = conversation.Participants
            .Select(p => new ParticipantDto(p.UserId.ToString(), p.DisplayName, p.Role.ToString()))
            .ToList();

        var dto = new ConversationDetailDto
        {
            Id           = conversation.Id.ToString(),
            Title        = conversation.Title,
            ContextType  = conversation.ContextType.ToString(),
            ContextId    = conversation.ContextId.ToString(),
            Status       = conversation.Status.ToString(),
            Participants = participants,
            Messages     = messages,
        };

        return new GetConversationDetailResponse(dto);
    }
}
