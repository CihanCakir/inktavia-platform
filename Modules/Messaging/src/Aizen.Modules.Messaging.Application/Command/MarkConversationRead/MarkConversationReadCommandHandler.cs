using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Messaging.Application.Realtime;
using Aizen.Modules.Messaging.Domain.Interface.Repository;

namespace Aizen.Modules.Messaging.Application.Command.MarkConversationRead;

[DocumentationInfo("Mark conversation read command handler",
    "Marks all messages in a conversation as read by admin and broadcasts unread count update.")]
public sealed class MarkConversationReadCommandHandler
    : AizenCommandHandler<MarkConversationReadCommand, bool>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly MessagingRealtimePublisher _realtimePublisher;

    public MarkConversationReadCommandHandler(
        IConversationRepository conversationRepository,
        MessagingRealtimePublisher realtimePublisher)
    {
        _conversationRepository = conversationRepository;
        _realtimePublisher      = realtimePublisher;
    }

    public override async Task<bool> Handle(
        MarkConversationReadCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken)
            ?? throw new InvalidOperationException($"Conversation {request.ConversationId} not found.");

        conversation.MarkReadByAdmin();
        _conversationRepository.Update(conversation);

        await _realtimePublisher.PublishConversationStatusChangedAsync(
            conversation.Id, "ReadByAdmin", cancellationToken);

        return true;
    }
}
