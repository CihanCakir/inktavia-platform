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
/// N4 (§9) — tells the provider their offer boost was cancelled (refund), through the N-B path. Requires the seeded
/// <c>PREMIUM_BOOST_REVOKED_INAPP</c> template.
/// </summary>
public sealed class PremiumBoostRevokedConsumer
    : AizenBaseMessageConsumer<PremiumBoostRevokedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<PremiumBoostRevokedConsumer> _logger;

    public PremiumBoostRevokedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<PremiumBoostRevokedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(PremiumBoostRevokedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(PremiumBoostRevokedMessage message, CancellationToken ct)
    {
        if (message.ProviderProfileId == 0) return;

        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.ProviderProfileId,
            Type            = NotificationType.PremiumBoostRevoked,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                ["offerId"] = message.OfferId.ToString(),
            },
            MetadataJson  = $"{{\"offerId\":{message.OfferId},\"entitlementId\":{message.EntitlementId}}}",
            ReferenceType = "PremiumEntitlement",
            ReferenceId   = message.OfferId,
        }, ct);

        _logger.LogInformation(
            "PremiumBoostRevoked offer={OfferId} → provider {ProfileId} notified.",
            message.OfferId, message.ProviderProfileId);
    }

    public override Task ExecuteRollbackMessage(PremiumBoostRevokedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: PremiumBoostRevokedConsumer offer={OfferId}: {Error}", message.OfferId, ex.Message);
        return Task.CompletedTask;
    }
}
