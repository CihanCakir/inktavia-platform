using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Identity.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.Identity;

public sealed class ProviderProfileRejectedConsumer
    : AizenBaseMessageConsumer<ProviderProfileRejectedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<ProviderProfileRejectedConsumer> _logger;

    public ProviderProfileRejectedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<ProviderProfileRejectedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ProviderProfileRejectedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ProviderProfileRejectedMessage message, CancellationToken ct)
    {
        var variables = new Dictionary<string, string>
        {
            ["profileType"] = message.ProfileType,
            ["reason"] = message.Reason,
        };
        var metadata = BuildMetadata(message);

        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.UserId,
            Type = NotificationType.ProfileRejected,
            Channel = NotificationChannel.InApp,
            Variables = variables,
            MetadataJson = metadata,
        }, ct);

        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.UserId,
            Type = NotificationType.ProfileRejected,
            Channel = NotificationChannel.Push,
            Variables = variables,
            MetadataJson = metadata,
        }, ct);

        _logger.LogInformation("ProfileRejected notification sent for user {UserId}, profile {ProfileId}.",
            message.UserId, message.ProfileId);
    }

    private static string BuildMetadata(ProviderProfileRejectedMessage message)
        => System.Text.Json.JsonSerializer.Serialize(new
        {
            profileId = message.ProfileId,
            profileType = message.ProfileType,
            reason = message.Reason,
            reasonCategory = message.ReasonCategory,
        });

    public override Task ExecuteRollbackMessage(ProviderProfileRejectedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ProviderProfileRejectedConsumer for user {UserId}: {Error}", message.UserId, ex.Message);
        return Task.CompletedTask;
    }
}
