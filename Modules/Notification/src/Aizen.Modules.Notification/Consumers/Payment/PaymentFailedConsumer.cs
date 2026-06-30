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
/// Notifies the payer when a payment has failed or timed out.
/// Note: SR module handles the state machine transition — this consumer only sends the notification.
/// </summary>
public sealed class PaymentFailedConsumer
    : AizenBaseMessageConsumer<PaymentFailedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<PaymentFailedConsumer> _logger;

    public PaymentFailedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<PaymentFailedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(PaymentFailedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(PaymentFailedMessage message, CancellationToken ct)
    {
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.PayerProfileId,
            Type            = NotificationType.PaymentFailed,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                ["transactionCode"] = message.TransactionCode,
                ["amount"]          = message.GrossAmount.ToString("F2"),
                ["currency"]        = message.CurrencyCode,
                ["failureReason"]   = message.FailureReason,
                ["isRetryable"]     = message.IsRetryable ? "true" : "false",
                ["failedAt"]        = message.FailedAtUtc.ToString("dd MMM yyyy HH:mm"),
            },
            MetadataJson = $"{{\"transactionId\":{message.TransactionId},\"contextType\":\"{message.ContextType}\",\"contextId\":{message.ContextId},\"isRetryable\":{message.IsRetryable.ToString().ToLower()}}}",
        }, ct);

        _logger.LogInformation(
            "Notification sent for PaymentFailed: {TransactionCode} Reason={Reason}",
            message.TransactionCode, message.FailureReason);
    }

    public override Task ExecuteRollbackMessage(PaymentFailedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning(
            "Rollback: PaymentFailedConsumer for {TransactionCode}: {Error}",
            message.TransactionCode, ex.Message);
        return Task.CompletedTask;
    }
}
