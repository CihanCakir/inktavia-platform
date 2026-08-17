using Aizen.Core.CQRS.Handler;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Message;
using Aizen.Modules.Messaging.Application.Realtime;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.Messaging.Application.Command.ModerateMessage;

[DocumentationInfo("Moderate message command handler",
    "Applies a moderation verdict to a message and notifies the admin moderation channel.")]
public sealed class ModerateMessageCommandHandler
    : AizenCommandHandler<ModerateMessageCommand, bool>
{
    private readonly IConversationMessageRepository _messageRepository;
    private readonly MessagingRealtimePublisher _realtimePublisher;
    private readonly IServiceProvider _serviceProvider;

    public ModerateMessageCommandHandler(
        IConversationMessageRepository messageRepository,
        MessagingRealtimePublisher realtimePublisher,
        IServiceProvider serviceProvider)
    {
        _messageRepository = messageRepository;
        _realtimePublisher = realtimePublisher;
        _serviceProvider   = serviceProvider;
    }

    public override async Task<bool> Handle(
        ModerateMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await _messageRepository.GetByIdAsync(request.MessageId, cancellationToken)
            ?? throw new InvalidOperationException($"Message {request.MessageId} not found.");

        message.SetModerationStatus(request.Status, request.Reason);
        _messageRepository.Update(message);

        // Retained module-hub realtime for the Blocked verdict (unchanged).
        if (request.Status == MessageModerationStatus.Blocked)
            await _realtimePublisher.PublishModerationEventAsync(
                message.ConversationId, message.Id,
                "BLOCKED", request.Reason ?? "Admin blocked message", cancellationToken);

        // Bus event that reaches the AdminPanel BFF realtime edge → live moderation queue. Published for EVERY
        // verdict (allow/flag/block/pending) so the admin queue refreshes live regardless of the outcome. Thin.
        var publisher = _serviceProvider.GetRequiredService<IAizenMessagePublisher>();
        await publisher.PublishAsync(new MessagingModerationEventMessage
        {
            ConversationId = message.ConversationId,
            MessageId      = message.Id,
            Kind           = "Moderated",
            NewStatus      = request.Status.ToString(),
            OccurredAt     = DateTimeOffset.UtcNow,
        }, cancellationToken);

        return true;
    }
}
