using System.Globalization;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using Aizen.Modules.Payment.Abstraction.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.Payment;

/// <summary>
/// N4 (§9) — tells the provider their offer boost is active (until {date}), through the N-B path. Requires the seeded
/// <c>PREMIUM_BOOST_ACTIVATED_INAPP</c> template.
/// </summary>
public sealed class PremiumBoostActivatedConsumer
    : AizenBaseMessageConsumer<PremiumBoostActivatedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<PremiumBoostActivatedConsumer> _logger;

    public PremiumBoostActivatedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<PremiumBoostActivatedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(PremiumBoostActivatedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(PremiumBoostActivatedMessage message, CancellationToken ct)
    {
        if (message.ProviderProfileId == 0) return;

        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.ProviderProfileId,
            Type            = NotificationType.PremiumBoostActivated,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                ["offerId"]   = message.OfferId.ToString(),
                ["expiresAt"] = message.ExpiresAtUtc.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
            },
            MetadataJson  = $"{{\"offerId\":{message.OfferId},\"entitlementId\":{message.EntitlementId}}}",
            ReferenceType = "PremiumEntitlement",
            ReferenceId   = message.OfferId,
        }, ct);

        _logger.LogInformation(
            "PremiumBoostActivated offer={OfferId} → provider {ProfileId} notified (until {Expires:o}).",
            message.OfferId, message.ProviderProfileId, message.ExpiresAtUtc);
    }

    public override Task ExecuteRollbackMessage(PremiumBoostActivatedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: PremiumBoostActivatedConsumer offer={OfferId}: {Error}", message.OfferId, ex.Message);
        return Task.CompletedTask;
    }
}
