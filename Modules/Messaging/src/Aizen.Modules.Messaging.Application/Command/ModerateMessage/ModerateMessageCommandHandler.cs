using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Application.Realtime;
using Aizen.Modules.Messaging.Domain.Interface.Repository;

namespace Aizen.Modules.Messaging.Application.Command.ModerateMessage;

[DocumentationInfo("Moderate message command handler",
    "Applies a moderation verdict to a message and notifies the admin moderation channel.")]
public sealed class ModerateMessageCommandHandler
    : AizenCommandHandler<ModerateMessageCommand, bool>
{
    private readonly IConversationMessageRepository _messageRepository;
    private readonly MessagingRealtimePublisher _realtimePublisher;

    public ModerateMessageCommandHandler(
        IConversationMessageRepository messageRepository,
        MessagingRealtimePublisher realtimePublisher)
    {
        _messageRepository = messageRepository;
        _realtimePublisher = realtimePublisher;
    }

    public override async Task<bool> Handle(
        ModerateMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await _messageRepository.GetByIdAsync(request.MessageId, cancellationToken)
            ?? throw new InvalidOperationException($"Message {request.MessageId} not found.");

        message.SetModerationStatus(request.Status, request.Reason);
        _messageRepository.Update(message);

        if (request.Status == MessageModerationStatus.Blocked)
            await _realtimePublisher.PublishModerationEventAsync(
                message.ConversationId, message.Id,
                "BLOCKED", request.Reason ?? "Admin blocked message", cancellationToken);

        return true;
    }
}
