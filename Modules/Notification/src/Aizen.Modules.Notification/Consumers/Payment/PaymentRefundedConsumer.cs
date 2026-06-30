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
/// Notifies the payer when a full or partial refund has been processed.
/// </summary>
public sealed class PaymentRefundedConsumer
    : AizenBaseMessageConsumer<PaymentRefundedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<PaymentRefundedConsumer> _logger;

    public PaymentRefundedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<PaymentRefundedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(PaymentRefundedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(PaymentRefundedMessage message, CancellationToken ct)
    {
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.PayerProfileId,
            Type            = NotificationType.PaymentRefunded,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                ["transactionCode"] = message.TransactionCode,
                ["refundedAmount"]  = message.RefundedAmount.ToString("F2"),
                ["originalAmount"]  = message.OriginalAmount.ToString("F2"),
                ["currency"]        = message.CurrencyCode,
                ["isPartial"]       = message.IsPartial ? "partial" : "full",
                ["reason"]          = message.Reason,
                ["refundedAt"]      = message.RefundedAtUtc.ToString("dd MMM yyyy HH:mm"),
            },
            MetadataJson = $"{{\"transactionId\":{message.TransactionId},\"contextType\":\"{message.ContextType}\",\"contextId\":{message.ContextId}}}",
        }, ct);

        _logger.LogInformation(
            "Notification sent for PaymentRefunded: {TransactionCode} Amount={Amount}",
            message.TransactionCode, message.RefundedAmount);
    }

    public override Task ExecuteRollbackMessage(PaymentRefundedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning(
            "Rollback: PaymentRefundedConsumer for {TransactionCode}: {Error}",
            message.TransactionCode, ex.Message);
        return Task.CompletedTask;
    }
}
