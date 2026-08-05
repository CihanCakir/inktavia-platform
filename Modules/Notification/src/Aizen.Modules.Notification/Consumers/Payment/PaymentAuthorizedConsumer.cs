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
/// N-C latent consumer — notifies the payer when a payment authorization hold ("Ödeme Provizyonda") is placed
/// (PreAuth, not yet captured). Mirrors <c>PaymentCapturedConsumer</c>. LATENT: won't fire until Payment BE-P9
/// publishes <see cref="PaymentAuthorizedMessage"/> (the authorize-only flow isn't built yet). Compiled + wired now so
/// it's ready; inherits N-B gating (PaymentAuthorized → Payments category).
/// </summary>
public sealed class PaymentAuthorizedConsumer
    : AizenBaseMessageConsumer<PaymentAuthorizedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<PaymentAuthorizedConsumer> _logger;

    public PaymentAuthorizedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<PaymentAuthorizedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(PaymentAuthorizedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(PaymentAuthorizedMessage message, CancellationToken ct)
    {
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.PayerProfileId,
            Type            = NotificationType.PaymentAuthorized,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                ["transactionCode"] = message.TransactionCode,
                ["amount"]          = message.AuthorizedAmount.ToString("F2"),
                ["currency"]        = message.CurrencyCode,
                ["authorizedAt"]    = message.AuthorizedAtUtc.ToString("dd MMM yyyy HH:mm"),
            },
            MetadataJson = $"{{\"transactionId\":{message.TransactionId},\"contextType\":\"{message.ContextType}\",\"contextId\":{message.ContextId}}}",
            ReferenceType = "Payment",
            ReferenceId   = message.TransactionId,
        }, ct);

        _logger.LogInformation(
            "Notification sent for PaymentAuthorized (PreAuth hold): {TransactionCode}", message.TransactionCode);
    }

    public override Task ExecuteRollbackMessage(PaymentAuthorizedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning(
            "Rollback: PaymentAuthorizedConsumer for {TransactionCode}: {Error}",
            message.TransactionCode, ex.Message);
        return Task.CompletedTask;
    }
}
