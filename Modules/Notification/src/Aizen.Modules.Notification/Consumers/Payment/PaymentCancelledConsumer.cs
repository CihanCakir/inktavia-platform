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
/// Notifies the payer when a pending payment intent has been cancelled before capture.
/// </summary>
public sealed class PaymentCancelledConsumer
    : AizenBaseMessageConsumer<PaymentCancelledMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<PaymentCancelledConsumer> _logger;

    public PaymentCancelledConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<PaymentCancelledConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(PaymentCancelledMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(PaymentCancelledMessage message, CancellationToken ct)
    {
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.PayerProfileId,
            Type            = NotificationType.PaymentCancelled,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                ["transactionCode"]    = message.TransactionCode,
                ["amount"]             = message.GrossAmount.ToString("F2"),
                ["currency"]           = message.CurrencyCode,
                ["cancellationReason"] = message.CancellationReason,
                ["cancelledAt"]        = message.CancelledAtUtc.ToString("dd MMM yyyy HH:mm"),
            },
            MetadataJson = $"{{\"transactionId\":{message.TransactionId},\"contextType\":\"{message.ContextType}\",\"contextId\":{message.ContextId}}}",
            ReferenceType = "Payment",
            ReferenceId   = message.TransactionId,
        }, ct);

        _logger.LogInformation(
            "Notification sent for PaymentCancelled: {TransactionCode}", message.TransactionCode);
    }

    public override Task ExecuteRollbackMessage(PaymentCancelledMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning(
            "Rollback: PaymentCancelledConsumer for {TransactionCode}: {Error}",
            message.TransactionCode, ex.Message);
        return Task.CompletedTask;
    }
}
