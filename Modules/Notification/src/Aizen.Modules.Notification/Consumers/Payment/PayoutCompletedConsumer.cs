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
/// Notifies the provider when a payout has been successfully processed by the gateway.
/// </summary>
public sealed class PayoutCompletedConsumer
    : AizenBaseMessageConsumer<PayoutCompletedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<PayoutCompletedConsumer> _logger;

    public PayoutCompletedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<PayoutCompletedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(PayoutCompletedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(PayoutCompletedMessage message, CancellationToken ct)
    {
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.ProviderProfileId,
            Type            = NotificationType.PayoutCompleted,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                ["amount"]           = message.Amount.ToString("F2"),
                ["currency"]         = message.CurrencyCode,
                ["gatewayProvider"]  = message.GatewayProvider,
                ["processedAt"]      = message.ProcessedAtUtc.ToString("dd MMM yyyy HH:mm"),
            },
            MetadataJson = $"{{\"payoutRecordId\":{message.PayoutRecordId},\"transactionId\":{message.TransactionId}}}",
        }, ct);

        _logger.LogInformation(
            "Notification sent for PayoutCompleted: PayoutRecord={PayoutRecordId} Amount={Amount}",
            message.PayoutRecordId, message.Amount);
    }

    public override Task ExecuteRollbackMessage(PayoutCompletedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning(
            "Rollback: PayoutCompletedConsumer for PayoutRecord={PayoutRecordId}: {Error}",
            message.PayoutRecordId, ex.Message);
        return Task.CompletedTask;
    }
}
