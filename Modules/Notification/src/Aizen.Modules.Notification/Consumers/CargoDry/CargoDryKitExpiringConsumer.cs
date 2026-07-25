using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.CargoDry;

public sealed class CargoDryKitExpiringConsumer
    : AizenBaseMessageConsumer<CargoDryKitExpiringMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<CargoDryKitExpiringConsumer> _logger;

    public CargoDryKitExpiringConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<CargoDryKitExpiringConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(CargoDryKitExpiringMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(CargoDryKitExpiringMessage message, CancellationToken ct)
    {
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.OwnerUserId,
            Type            = NotificationType.CargoDryKitExpiringReminder,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                ["kitCode"]     = message.KitCode,
                ["daysLeft"]    = message.DaysLeft.ToString(),
                ["expiryDate"]  = message.ExpiresAt.ToString("dd MMM yyyy"),
                ["productName"] = message.ProductName,
            },
            MetadataJson = $"{{\"kitId\":{message.KitId}}}",
            ReferenceType = "CargoDry",
            ReferenceId   = message.KitId,
        }, ct);

        _logger.LogInformation("Notification sent for CargoDryKitExpiring: {KitCode}, DaysLeft={DaysLeft}", message.KitCode, message.DaysLeft);
    }

    public override Task ExecuteRollbackMessage(CargoDryKitExpiringMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: CargoDryKitExpiringConsumer for {KitCode}: {Error}", message.KitCode, ex.Message);
        return Task.CompletedTask;
    }
}
