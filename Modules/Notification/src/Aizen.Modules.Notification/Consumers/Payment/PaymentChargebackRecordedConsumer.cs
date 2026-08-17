using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using Aizen.Modules.Payment.Abstraction.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.Payment;

/// <summary>
/// N3-B — a gateway chargeback was recorded. Notifies the <b>provider</b> whose transaction was charged back (they see
/// the P10 clawback / negative-balance impact) and all <b>admins</b> (<see cref="NotificationType.ChargebackRecorded"/>
/// 159, Payments category → N-B gated). Admin ids come from Identity (best-effort).
/// </summary>
public sealed class PaymentChargebackRecordedConsumer
    : AizenBaseMessageConsumer<PaymentChargebackRecordedMessage>
{
    private readonly ISender _sender;
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly ILogger<PaymentChargebackRecordedConsumer> _logger;

    public PaymentChargebackRecordedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender   = sp.GetRequiredService<ISender>();
        _identity = sp.GetRequiredService<INotificationIdentityRemoteCall>();
        _logger   = sp.GetRequiredService<ILogger<PaymentChargebackRecordedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(PaymentChargebackRecordedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(PaymentChargebackRecordedMessage message, CancellationToken ct)
    {
        var recipients = new HashSet<long>();
        if (message.ProviderProfileId != 0) recipients.Add(message.ProviderProfileId);

        try
        {
            var admins = (await _identity.GetAdminUserIds()).Body ?? new List<long>();
            foreach (var adminId in admins) if (adminId != 0) recipients.Add(adminId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ChargebackRecorded Tx={TxId}: could not resolve admin ids; provider still notified.", message.TransactionId);
        }

        foreach (var userId in recipients)
        {
            await _sender.Send(new SendNotificationCommand
            {
                RecipientUserId = userId,
                Type            = NotificationType.ChargebackRecorded,
                Channel         = NotificationChannel.InApp,
                Variables = new Dictionary<string, string>
                {
                    { "transactionCode", message.TransactionCode },
                    { "amount",          message.Amount.ToString("F2") },
                    { "currency",        message.CurrencyCode },
                    { "gatewayRef",      message.GatewayChargebackReference },
                },
                MetadataJson  = $"{{\"transactionId\":{message.TransactionId},\"contextType\":\"{message.ContextType}\",\"contextId\":{message.ContextId}}}",
                ReferenceType = "Payment",
                ReferenceId   = message.TransactionId,
            }, ct);
        }

        _logger.LogInformation(
            "ChargebackRecorded Tx={TxId} Ref={Ref} → notified {Count} recipients (provider+admins).",
            message.TransactionId, message.GatewayChargebackReference, recipients.Count);
    }

    public override Task ExecuteRollbackMessage(PaymentChargebackRecordedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: PaymentChargebackRecordedConsumer Tx={TxId}: {Error}", message.TransactionId, ex.Message);
        return Task.CompletedTask;
    }
}
