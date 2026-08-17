using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Identity.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.Identity;

public sealed class ProviderOtpLoginOtpRequestedConsumer
    : AizenBaseMessageConsumer<ProviderOtpLoginOtpRequestedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<ProviderOtpLoginOtpRequestedConsumer> _logger;

    public ProviderOtpLoginOtpRequestedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<ProviderOtpLoginOtpRequestedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ProviderOtpLoginOtpRequestedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ProviderOtpLoginOtpRequestedMessage message, CancellationToken ct)
    {
        var channel = message.Channel?.ToLowerInvariant() == "phone" ? NotificationChannel.Sms : NotificationChannel.Email;

        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.RecipientUserId,
            Type = NotificationType.OtpLoginCode,
            Channel = channel,
            Variables = new Dictionary<string, string>
            {
                ["otp"] = message.Otp,
                ["expiresMinutes"] = message.ExpiresInMinutes.ToString(),
                ["maskedTarget"] = message.MaskedTarget,
            },
            MetadataJson = BuildMetadata(message),
        }, ct);

        _logger.LogInformation("Notification sent for OtpLoginCode to {MaskedTarget} via {Channel}.", message.MaskedTarget, channel);
    }

    private static string? BuildMetadata(ProviderOtpLoginOtpRequestedMessage message)
    {
        var parts = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(message.Email)) parts["recipientEmail"] = message.Email;
        if (!string.IsNullOrWhiteSpace(message.Phone)) parts["recipientPhone"] = message.Phone;
        return parts.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(parts) : null;
    }

    public override Task ExecuteRollbackMessage(ProviderOtpLoginOtpRequestedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ProviderOtpLoginOtpRequestedConsumer for {MaskedTarget}: {Error}", message.MaskedTarget, ex.Message);
        return Task.CompletedTask;
    }
}
