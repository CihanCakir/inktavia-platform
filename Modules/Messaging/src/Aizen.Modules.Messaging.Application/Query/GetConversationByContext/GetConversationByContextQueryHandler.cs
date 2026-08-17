using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Aizen.Modules.Messaging.Repository.Mapping;

namespace Aizen.Modules.Messaging.Application.Query.GetConversationByContext;

[DocumentationInfo("Get conversation by context query handler",
    "Looks up a conversation by domain context type and ID.")]
public sealed class GetConversationByContextQueryHandler
    : AizenQueryHandler<GetConversationByContextQuery, GetConversationDetailResponse>
{
    private readonly IConversationRepository _conversationRepository;

    public GetConversationByContextQueryHandler(IConversationRepository conversationRepository)
        => _conversationRepository = conversationRepository;

    public override async Task<GetConversationDetailResponse> Handle(
        GetConversationByContextQuery request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByContextAsync(
            request.ContextType, request.ContextId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"No conversation found for context {request.ContextType}:{request.ContextId}.");

        var messages = conversation.Messages
            .Where(m => !m.IsInternalNote)
            .Where(m => m.ModerationStatus != MessageModerationStatus.Blocked)
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
