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
/// Sends a payment reminder to the payer when a transaction has been pending too long.
/// Published by <c>PaymentReminderJob</c> on the Payment scheduler.
/// </summary>
public sealed class PaymentReminderRequestedConsumer
    : AizenBaseMessageConsumer<PaymentReminderRequestedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<PaymentReminderRequestedConsumer> _logger;

    public PaymentReminderRequestedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<PaymentReminderRequestedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(PaymentReminderRequestedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(PaymentReminderRequestedMessage message, CancellationToken ct)
    {
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.PayerProfileId,
            Type            = NotificationType.PaymentReminderDue,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                ["transactionCode"]  = message.TransactionCode,
                ["amount"]           = message.GrossAmount.ToString("F2"),
                ["currency"]         = message.CurrencyCode,
                ["reminderWindow"]   = message.ReminderWindow,
                ["pendingSinceUtc"]  = message.PendingSinceUtc.ToString("dd MMM yyyy HH:mm"),
            },
            MetadataJson = $"{{\"transactionId\":{message.TransactionId},\"contextType\":\"{message.ContextType}\",\"contextId\":{message.ContextId}}}",
        }, ct);

        _logger.LogInformation(
            "Notification sent for PaymentReminderRequested: {TransactionCode} Window={Window}",
            message.TransactionCode, message.ReminderWindow);
    }

    public override Task ExecuteRollbackMessage(PaymentReminderRequestedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning(
            "Rollback: PaymentReminderRequestedConsumer for {TransactionCode}: {Error}",
            message.TransactionCode, ex.Message);
        return Task.CompletedTask;
    }
}
