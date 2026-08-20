using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Identity.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.Identity;

/// <summary>
/// Identity'nin yayınladığı <see cref="ProviderEmailVerificationRequestedMessage"/>'ı PROVIDER_EMAIL_VERIFICATION
/// şablonlu bir e-postaya çevirir (Type=ProviderEmailVerification, Channel=Email — Account/security kategorisi,
/// tercihe takılmaz). Alıcı adresi MetadataJson'a <c>recipientEmail</c> olarak AÇIKÇA konur: yeni kaydolmuş,
/// henüz doğrulanmamış kullanıcı için EmailNotificationDispatcher'ın profil-fallback'ine güvenilmez.
/// </summary>
public sealed class ProviderEmailVerificationRequestedConsumer
    : AizenBaseMessageConsumer<ProviderEmailVerificationRequestedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<ProviderEmailVerificationRequestedConsumer> _logger;

    public ProviderEmailVerificationRequestedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<ProviderEmailVerificationRequestedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(
        ProviderEmailVerificationRequestedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(
        ProviderEmailVerificationRequestedMessage message, CancellationToken ct)
    {
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.RecipientUserId,
            Type = NotificationType.ProviderEmailVerification,
            Channel = NotificationChannel.Email,
            Variables = new Dictionary<string, string>
            {
                ["verifyUrl"] = message.VerifyUrl,
            },
            MetadataJson = BuildMetadata(message),
        }, ct);

        _logger.LogInformation(
            "Notification sent for ProviderEmailVerification to {MaskedTarget}.", message.MaskedTarget);
    }

    private static string BuildMetadata(ProviderEmailVerificationRequestedMessage message)
    {
        // Alıcı adresi açıkça geçilir (dispatcher önce MetadataJson'daki recipientEmail'e bakar, ancak sonra
        // profilden çözmeye düşer — doğrulanmamış yeni kullanıcı için bu fallback güvenilir değildir).
        var parts = new Dictionary<string, string> { ["recipientEmail"] = message.Email };
        return System.Text.Json.JsonSerializer.Serialize(parts);
    }

    public override Task ExecuteRollbackMessage(
        ProviderEmailVerificationRequestedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning(
            "Rollback: ProviderEmailVerificationRequestedConsumer for {MaskedTarget}: {Error}",
            message.MaskedTarget, ex.Message);
        return Task.CompletedTask;
    }
}
