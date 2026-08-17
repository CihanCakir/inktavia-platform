using Aizen.Core.CQRS.Handler;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Messaging.Abstraction.Message;
using Aizen.Modules.Messaging.Application.Realtime;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.Messaging.Application.Command.FlagConversation;

[DocumentationInfo("Flag conversation command handler",
    "Sets conversation status to Flagged and notifies participants via realtime.")]
public sealed class FlagConversationCommandHandler
    : AizenCommandHandler<FlagConversationCommand, bool>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly MessagingRealtimePublisher _realtimePublisher;
    private readonly IServiceProvider _serviceProvider;

    public FlagConversationCommandHandler(
        IConversationRepository conversationRepository,
        MessagingRealtimePublisher realtimePublisher,
        IServiceProvider serviceProvider)
    {
        _conversationRepository = conversationRepository;
        _realtimePublisher      = realtimePublisher;
        _serviceProvider        = serviceProvider;
    }

    public override async Task<bool> Handle(
        FlagConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken)
            ?? throw new InvalidOperationException($"Conversation {request.ConversationId} not found.");

        conversation.Flag();
        _conversationRepository.Update(conversation);

        // Retained module-hub realtime (participants), unchanged.
        await _realtimePublisher.PublishConversationStatusChangedAsync(
            conversation.Id, "Flagged", cancellationToken);

        // Bus event → AdminPanel BFF realtime edge → live moderation queue + conversation-status update. Thin.
        var publisher = _serviceProvider.GetRequiredService<IAizenMessagePublisher>();
        await publisher.PublishAsync(new MessagingModerationEventMessage
        {
            ConversationId = conversation.Id,
            MessageId      = null,
            Kind           = "Flagged",
            NewStatus      = "Flagged",
            OccurredAt     = DateTimeOffset.UtcNow,
        }, cancellationToken);

        return true;
    }
}
