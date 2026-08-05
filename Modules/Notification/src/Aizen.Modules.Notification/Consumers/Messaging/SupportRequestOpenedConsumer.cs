using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Messaging.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.Messaging;

/// <summary>
/// N-D — on a live-support request opening, notify every admin user (so a customer isn't missed). Resolves admin
/// user ids from Identity (internal read), then sends one <see cref="NotificationType.SupportRequestOpened"/>
/// (Broadcast category) notification per admin, SEQUENTIALLY (single scoped DbContext — mirrors MessagingMessageSent).
/// In-app always lands + the live badge; push only if the admin opted-in (N-B Broadcast gating, applied downstream).
/// </summary>
public sealed class SupportRequestOpenedConsumer
    : AizenBaseMessageConsumer<SupportRequestOpenedMessage>
{
    private readonly ISender _sender;
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly ILogger<SupportRequestOpenedConsumer> _logger;

    public SupportRequestOpenedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender   = sp.GetRequiredService<ISender>();
        _identity = sp.GetRequiredService<INotificationIdentityRemoteCall>();
        _logger   = sp.GetRequiredService<ILogger<SupportRequestOpenedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(SupportRequestOpenedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(SupportRequestOpenedMessage message, CancellationToken ct)
    {
        List<long> adminIds;
        try
        {
            var response = await _identity.GetAdminUserIds();
            adminIds = response.Body ?? new List<long>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not resolve admin user ids for SupportRequestOpened (conversation {ConvId}); admins not notified.",
                message.ConversationId);
            return;
        }

        if (adminIds.Count == 0)
        {
            _logger.LogWarning("No admin users resolved; SupportRequestOpened notification skipped for conversation {ConvId}.",
                message.ConversationId);
            return;
        }

        foreach (var adminId in adminIds)
        {
            await _sender.Send(new SendNotificationCommand
            {
                RecipientUserId = adminId,
                Type            = NotificationType.SupportRequestOpened,
                Channel         = NotificationChannel.InApp,
                Variables = new Dictionary<string, string>
                {
                    { "topic",   message.Topic.ToString() },
                    { "subject", message.Subject },
                    { "requesterName", message.RequesterName },
                },
                MetadataJson  = $"{{\"conversationId\":{message.ConversationId},\"topic\":\"{message.Topic}\"}}",
                ReferenceType = "SupportConversation",
                ReferenceId   = message.ConversationId,
            }, ct);
        }

        _logger.LogInformation(
            "SupportRequestOpened (conversation {ConvId}, topic {Topic}) → notified {Count} admins.",
            message.ConversationId, message.Topic, adminIds.Count);
    }

    public override Task ExecuteRollbackMessage(SupportRequestOpenedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: SupportRequestOpenedConsumer conversation {ConvId}: {Error}",
            message.ConversationId, ex.Message);
        return Task.CompletedTask;
    }
}
