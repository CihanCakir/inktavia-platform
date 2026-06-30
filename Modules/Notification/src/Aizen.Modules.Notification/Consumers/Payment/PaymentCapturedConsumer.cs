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
/// Notifies the payer when a payment has been successfully captured by the gateway.
/// </summary>
public sealed class PaymentCapturedConsumer
    : AizenBaseMessageConsumer<PaymentCapturedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<PaymentCapturedConsumer> _logger;

    public PaymentCapturedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<PaymentCapturedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(PaymentCapturedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(PaymentCapturedMessage message, CancellationToken ct)
    {
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.PayerProfileId,
            Type            = NotificationType.PaymentCaptured,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                ["transactionCode"] = message.TransactionCode,
                ["amount"]          = message.GrossAmount.ToString("F2"),
                ["currency"]        = message.CurrencyCode,
                ["capturedAt"]      = message.CapturedAtUtc.ToString("dd MMM yyyy HH:mm"),
            },
            MetadataJson = $"{{\"transactionId\":{message.TransactionId},\"contextType\":\"{message.ContextType}\",\"contextId\":{message.ContextId}}}",
        }, ct);

        _logger.LogInformation(
            "Notification sent for PaymentCaptured: {TransactionCode}", message.TransactionCode);
    }

    public override Task ExecuteRollbackMessage(PaymentCapturedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning(
            "Rollback: PaymentCapturedConsumer for {TransactionCode}: {Error}",
            message.TransactionCode, ex.Message);
        return Task.CompletedTask;
    }
}
