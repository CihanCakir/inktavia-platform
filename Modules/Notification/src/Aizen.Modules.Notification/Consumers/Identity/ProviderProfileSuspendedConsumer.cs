using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Identity.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.Identity;

public sealed class ProviderProfileSuspendedConsumer
    : AizenBaseMessageConsumer<ProviderProfileSuspendedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<ProviderProfileSuspendedConsumer> _logger;

    public ProviderProfileSuspendedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<ProviderProfileSuspendedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ProviderProfileSuspendedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ProviderProfileSuspendedMessage message, CancellationToken ct)
    {
        var variables = new Dictionary<string, string>
        {
            ["reason"] = message.Reason,
        };
        var metadata = BuildMetadata(message);

        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.UserId,
            Type = NotificationType.ProfileSuspended,
            Channel = NotificationChannel.InApp,
            Variables = variables,
            MetadataJson = metadata,
        }, ct);

        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.UserId,
            Type = NotificationType.ProfileSuspended,
            Channel = NotificationChannel.Push,
            Variables = variables,
            MetadataJson = metadata,
        }, ct);

        _logger.LogInformation("ProfileSuspended notification sent for user {UserId}, profile {ProfileId}.",
            message.UserId, message.ProfileId);
    }

    private static string BuildMetadata(ProviderProfileSuspendedMessage message)
        => System.Text.Json.JsonSerializer.Serialize(new
        {
            profileId = message.ProfileId,
            reason = message.Reason,
        });

    public override Task ExecuteRollbackMessage(ProviderProfileSuspendedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ProviderProfileSuspendedConsumer for user {UserId}: {Error}", message.UserId, ex.Message);
        return Task.CompletedTask;
    }
}
