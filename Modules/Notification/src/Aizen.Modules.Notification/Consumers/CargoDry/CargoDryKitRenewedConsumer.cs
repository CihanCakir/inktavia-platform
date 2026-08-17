using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.CargoDry;

public sealed class CargoDryKitRenewedConsumer
    : AizenBaseMessageConsumer<CargoDryKitRenewedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<CargoDryKitRenewedConsumer> _logger;

    public CargoDryKitRenewedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<CargoDryKitRenewedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(CargoDryKitRenewedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(CargoDryKitRenewedMessage message, CancellationToken ct)
    {
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.OwnerUserId,
            Type            = NotificationType.CargoDryKitRenewed,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                ["kitCode"]       = message.KitCode,
                ["newExpiryDate"] = message.NewExpiresAt.ToString("dd MMM yyyy"),
                ["renewalType"]   = message.RenewalType,
            },
            MetadataJson = $"{{\"kitId\":{message.KitId}}}",
            ReferenceType = "CargoDry",
            ReferenceId   = message.KitId,
        }, ct);

        _logger.LogInformation("Notification sent for CargoDryKitRenewed: {KitCode}", message.KitCode);
    }

    public override Task ExecuteRollbackMessage(CargoDryKitRenewedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: CargoDryKitRenewedConsumer for {KitCode}: {Error}", message.KitCode, ex.Message);
        return Task.CompletedTask;
    }
}
