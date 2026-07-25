using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.CargoDry;

public sealed class CargoDryKitActivatedConsumer
    : AizenBaseMessageConsumer<CargoDryKitActivatedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<CargoDryKitActivatedConsumer> _logger;

    public CargoDryKitActivatedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<CargoDryKitActivatedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(CargoDryKitActivatedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(CargoDryKitActivatedMessage message, CancellationToken ct)
    {
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.OwnerUserId,
            Type            = NotificationType.CargoDryKitActivated,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                ["kitCode"]      = message.KitCode,
                ["serialNumber"] = message.SerialNumber,
                ["productName"]  = message.ProductName,
                ["expiryDate"]   = message.ExpiresAt.ToString("dd MMM yyyy"),
            },
            MetadataJson = $"{{\"kitId\":{message.KitId}}}",
            ReferenceType = "CargoDry",
            ReferenceId   = message.KitId,
        }, ct);

        _logger.LogInformation("Notification sent for CargoDryKitActivated: {KitCode}", message.KitCode);
    }

    public override Task ExecuteRollbackMessage(CargoDryKitActivatedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: CargoDryKitActivatedConsumer for {KitCode}: {Error}", message.KitCode, ex.Message);
        return Task.CompletedTask;
    }
}
