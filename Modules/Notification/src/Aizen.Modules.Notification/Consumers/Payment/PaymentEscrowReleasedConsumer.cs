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
/// Notifies the provider when escrowed funds are released (SR completion approved).
/// Uses <see cref="NotificationType.PaymentReleased"/> — the existing enum value for this flow.
/// </summary>
public sealed class PaymentEscrowReleasedConsumer
    : AizenBaseMessageConsumer<PaymentEscrowReleasedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<PaymentEscrowReleasedConsumer> _logger;

    public PaymentEscrowReleasedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<PaymentEscrowReleasedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(PaymentEscrowReleasedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(PaymentEscrowReleasedMessage message, CancellationToken ct)
    {
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.RecipientProfileId,
            Type            = NotificationType.PaymentReleased,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                ["transactionCode"]  = message.TransactionCode,
                ["netPayoutAmount"]  = message.NetPayoutAmount.ToString("F2"),
                ["commissionAmount"] = message.CommissionAmount.ToString("F2"),
                ["currency"]         = message.CurrencyCode,
                ["releasedAt"]       = message.ReleasedAtUtc.ToString("dd MMM yyyy HH:mm"),
            },
            MetadataJson = $"{{\"transactionId\":{message.TransactionId},\"contextType\":\"{message.ContextType}\",\"contextId\":{message.ContextId}}}",
        }, ct);

        _logger.LogInformation(
            "Notification sent for PaymentEscrowReleased: {TransactionCode} Net={Net}",
            message.TransactionCode, message.NetPayoutAmount);
    }

    public override Task ExecuteRollbackMessage(PaymentEscrowReleasedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning(
            "Rollback: PaymentEscrowReleasedConsumer for {TransactionCode}: {Error}",
            message.TransactionCode, ex.Message);
        return Task.CompletedTask;
    }
}
