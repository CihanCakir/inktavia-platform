using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Messaging.Application.Realtime;
using Aizen.Modules.Messaging.Domain.Interface.Repository;

namespace Aizen.Modules.Messaging.Application.Command.FlagConversation;

[DocumentationInfo("Flag conversation command handler",
    "Sets conversation status to Flagged and notifies participants via realtime.")]
public sealed class FlagConversationCommandHandler
    : AizenCommandHandler<FlagConversationCommand, bool>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly MessagingRealtimePublisher _realtimePublisher;

    public FlagConversationCommandHandler(
        IConversationRepository conversationRepository,
        MessagingRealtimePublisher realtimePublisher)
    {
        _conversationRepository = conversationRepository;
        _realtimePublisher      = realtimePublisher;
    }

    public override async Task<bool> Handle(
        FlagConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken)
            ?? throw new InvalidOperationException($"Conversation {request.ConversationId} not found.");

        conversation.Flag();
        _conversationRepository.Update(conversation);

        await _realtimePublisher.PublishConversationStatusChangedAsync(
            conversation.Id, "Flagged", cancellationToken);

        return true;
    }
}
