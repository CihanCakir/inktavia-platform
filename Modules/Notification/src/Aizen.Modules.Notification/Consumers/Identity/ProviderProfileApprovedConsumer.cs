using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Identity.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.Identity;

public sealed class ProviderProfileApprovedConsumer
    : AizenBaseMessageConsumer<ProviderProfileApprovedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<ProviderProfileApprovedConsumer> _logger;

    public ProviderProfileApprovedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<ProviderProfileApprovedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ProviderProfileApprovedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ProviderProfileApprovedMessage message, CancellationToken ct)
    {
        var variables = new Dictionary<string, string>
        {
            ["profileType"] = message.ProfileType,
        };
        var metadata = BuildMetadata(message);

        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.UserId,
            Type = NotificationType.ProfileApproved,
            Channel = NotificationChannel.InApp,
            Variables = variables,
            MetadataJson = metadata,
        }, ct);

        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.UserId,
            Type = NotificationType.ProfileApproved,
            Channel = NotificationChannel.Push,
            Variables = variables,
            MetadataJson = metadata,
        }, ct);

        _logger.LogInformation("ProfileApproved notification sent for user {UserId}, profile {ProfileId}.",
            message.UserId, message.ProfileId);
    }

    private static string BuildMetadata(ProviderProfileApprovedMessage message)
        => System.Text.Json.JsonSerializer.Serialize(new
        {
            profileId = message.ProfileId,
            profileType = message.ProfileType,
        });

    public override Task ExecuteRollbackMessage(ProviderProfileApprovedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ProviderProfileApprovedConsumer for user {UserId}: {Error}", message.UserId, ex.Message);
        return Task.CompletedTask;
    }
}
