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
/// N1 (§13.2) — reminds the provider that their subscription's renewal price changes soon, through the N-B path
/// (InApp baseline; push/email honour the Payments-category preference). Requires the seeded
/// <c>SUB_PRICE_CHANGE_UPCOMING_INAPP</c> template.
/// </summary>
public sealed class SubscriptionPriceChangeUpcomingConsumer
    : AizenBaseMessageConsumer<SubscriptionPriceChangeUpcomingMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<SubscriptionPriceChangeUpcomingConsumer> _logger;

    public SubscriptionPriceChangeUpcomingConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<SubscriptionPriceChangeUpcomingConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(SubscriptionPriceChangeUpcomingMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(SubscriptionPriceChangeUpcomingMessage message, CancellationToken ct)
    {
        if (message.ProviderProfileId == 0) return;

        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.ProviderProfileId,
            Type            = NotificationType.SubscriptionPriceChangeUpcoming,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                ["plan"]         = message.PlanCode,
                ["currentPrice"] = message.CurrentPrice.ToString("F2", CultureInfo.InvariantCulture),
                ["newPrice"]     = message.NewPrice.ToString("F2", CultureInfo.InvariantCulture),
                ["currency"]     = message.CurrencyCode,
                ["date"]         = message.EffectiveAtUtc.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
            },
            MetadataJson  = $"{{\"subscriptionId\":{message.SubscriptionId},\"planCode\":\"{message.PlanCode}\"}}",
            ReferenceType = "ProviderSubscription",
            ReferenceId   = message.SubscriptionId,
        }, ct);

        _logger.LogInformation(
            "SubscriptionPriceChangeUpcoming sub={SubId} plan={Plan} → provider {ProfileId} notified ({Cur}→{New}).",
            message.SubscriptionId, message.PlanCode, message.ProviderProfileId, message.CurrentPrice, message.NewPrice);
    }

    public override Task ExecuteRollbackMessage(
        SubscriptionPriceChangeUpcomingMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: SubscriptionPriceChangeUpcomingConsumer sub={SubId}: {Error}",
            message.SubscriptionId, ex.Message);
        return Task.CompletedTask;
    }
}
