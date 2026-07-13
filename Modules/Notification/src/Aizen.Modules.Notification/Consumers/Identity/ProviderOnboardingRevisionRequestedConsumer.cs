using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Identity.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.Identity;

public sealed class ProviderOnboardingRevisionRequestedConsumer
    : AizenBaseMessageConsumer<ProviderOnboardingRevisionRequestedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<ProviderOnboardingRevisionRequestedConsumer> _logger;

    public ProviderOnboardingRevisionRequestedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<ProviderOnboardingRevisionRequestedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ProviderOnboardingRevisionRequestedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ProviderOnboardingRevisionRequestedMessage message, CancellationToken ct)
    {
        var stepsText = string.Join(", ", message.Steps);
        var variables = new Dictionary<string, string>
        {
            ["steps"] = stepsText,
            ["note"] = message.Note ?? "",
        };
        var metadata = BuildMetadata(message);

        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.UserId,
            Type = NotificationType.OnboardingRevisionRequested,
            Channel = NotificationChannel.InApp,
            Variables = variables,
            MetadataJson = metadata,
        }, ct);

        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.UserId,
            Type = NotificationType.OnboardingRevisionRequested,
            Channel = NotificationChannel.Push,
            Variables = variables,
            MetadataJson = metadata,
        }, ct);

        _logger.LogInformation("OnboardingRevisionRequested notification sent for user {UserId}, profile {ProfileId}, steps: {Steps}.",
            message.UserId, message.ProfileId, stepsText);
    }

    private static string BuildMetadata(ProviderOnboardingRevisionRequestedMessage message)
        => System.Text.Json.JsonSerializer.Serialize(new
        {
            profileId = message.ProfileId,
            steps = message.Steps,
            note = message.Note,
        });

    public override Task ExecuteRollbackMessage(ProviderOnboardingRevisionRequestedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ProviderOnboardingRevisionRequestedConsumer for user {UserId}: {Error}", message.UserId, ex.Message);
        return Task.CompletedTask;
    }
}
