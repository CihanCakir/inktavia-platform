using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Messaging.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.Messaging;

public sealed class MessagingMessageSentConsumer
    : AizenBaseMessageConsumer<MessagingMessageSentMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<MessagingMessageSentConsumer> _logger;

    public MessagingMessageSentConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<MessagingMessageSentConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(MessagingMessageSentMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(MessagingMessageSentMessage message, CancellationToken ct)
    {
        if (message.RecipientUserIds.Count == 0)
        {
            _logger.LogDebug("MessagingMessageSentConsumer: no recipients for conversation {ConvId}", message.ConversationId);
            return;
        }

        var metadataJson = $"{{\"conversationId\":{message.ConversationId}}}";

        // Send per-recipient notifications SEQUENTIALLY. The prior Task.WhenAll fired every recipient's
        // SendNotificationCommand concurrently over this consumer's single scoped DbContext, which for a
        // conversation with >1 recipient races EF Core ("The connection is already in a transaction and
        // cannot participate in another transaction") → the commit throws → the racing recipients LOSE their
        // notification. (Latent pre-WS2: the two-phase double-commit's second delivery attempt masked it;
        // exactly-once exposed it.) A sequential loop lets each command complete before the next starts, so
        // every recipient is notified exactly once. Behavior is otherwise identical.
        foreach (var recipientId in message.RecipientUserIds)
        {
            await _sender.Send(new SendNotificationCommand
            {
                RecipientUserId = recipientId,
                Type            = NotificationType.NewMessageReceived,
                Channel         = NotificationChannel.InApp,
                Variables = new Dictionary<string, string>
                {
                    { "senderName",        message.SenderName },
                    { "conversationTitle", message.ConversationTitle },
                },
                MetadataJson  = metadataJson,
                ReferenceType = "Message",
                ReferenceId   = message.ConversationId,
            }, ct);
        }
    }

    public override Task ExecuteRollbackMessage(MessagingMessageSentMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: MessagingMessageSentConsumer conv={ConvId}: {Error}", message.ConversationId, ex.Message);
        return Task.CompletedTask;
    }
}
