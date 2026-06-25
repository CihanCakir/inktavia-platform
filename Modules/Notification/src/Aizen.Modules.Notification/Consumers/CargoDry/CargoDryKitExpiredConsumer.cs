using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.CargoDry;

public sealed class CargoDryKitExpiredConsumer
    : AizenBaseMessageConsumer<CargoDryKitExpiredMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<CargoDryKitExpiredConsumer> _logger;

    public CargoDryKitExpiredConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<CargoDryKitExpiredConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(CargoDryKitExpiredMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(CargoDryKitExpiredMessage message, CancellationToken ct)
    {
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.OwnerUserId,
            Type            = NotificationType.CargoDryKitExpired,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                ["kitCode"] = message.KitCode,
            },
            MetadataJson = $"{{\"kitId\":{message.KitId}}}",
        }, ct);

        _logger.LogInformation("Notification sent for CargoDryKitExpired: {KitCode}", message.KitCode);
    }

    public override Task ExecuteRollbackMessage(CargoDryKitExpiredMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: CargoDryKitExpiredConsumer for {KitCode}: {Error}", message.KitCode, ex.Message);
        return Task.CompletedTask;
    }
}
