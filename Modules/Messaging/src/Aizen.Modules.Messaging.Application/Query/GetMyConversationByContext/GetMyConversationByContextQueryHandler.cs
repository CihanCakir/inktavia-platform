using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Aizen.Modules.Messaging.Repository.Mapping;

namespace Aizen.Modules.Messaging.Application.Query.GetMyConversationByContext;

[DocumentationInfo("Get my conversation-by-context query handler",
    "Loads the thread for a domain context, authorizes the authenticated caller as a participant, and returns the " +
    "non-admin view (internal notes + blocked messages excluded), chronological.")]
public sealed class GetMyConversationByContextQueryHandler
    : AizenQueryHandler<GetMyConversationByContextQuery, GetConversationDetailResponse>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IAizenInfoAccessor _info;

    public GetMyConversationByContextQueryHandler(
        IConversationRepository conversationRepository,
        IAizenInfoAccessor info)
    {
        _conversationRepository = conversationRepository;
        _info                   = info;
    }

    public override async Task<GetConversationDetailResponse> Handle(
        GetMyConversationByContextQuery request, CancellationToken cancellationToken)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;

        var conversation = await _conversationRepository.GetByContextWithMessagesAsync(
            request.ContextType, request.ContextId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"No conversation found for context {request.ContextType}:{request.ContextId}.");

        // PARTICIPANT-AUTHORIZATION: the authenticated caller must be a participant of this conversation. This is
        // the guard that prevents a provider from reading a thread they don't belong to (userId is the asserted
        // principal, never a client value).
        if (!conversation.Participants.Any(p => p.UserId == userId))
            throw new UnauthorizedAccessException(
                $"User {userId} is not a participant of conversation {conversation.Id}.");

        // Non-admin view: exclude soft-deleted, internal admin notes, and blocked messages; oldest-first render.
        var messages = conversation.Messages
            .Where(m => !m.IsDeleted)
            .Where(m => !m.IsInternalNote)
            .Where(m => m.ModerationStatus != MessageModerationStatus.Blocked)
            .OrderBy(m => m.SentAt)
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
