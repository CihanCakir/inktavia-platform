using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.CargoDry;

public sealed class CargoDryKitRevokedConsumer
    : AizenBaseMessageConsumer<CargoDryKitRevokedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<CargoDryKitRevokedConsumer> _logger;

    public CargoDryKitRevokedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<CargoDryKitRevokedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(CargoDryKitRevokedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(CargoDryKitRevokedMessage message, CancellationToken ct)
    {
        if (message.OwnerUserId is null) return; // unassigned kit, skip

        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.OwnerUserId.Value,
            Type            = NotificationType.CargoDryKitRevoked,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                ["kitCode"]   = message.KitCode,
                ["revokedAt"] = DateTimeOffset.UtcNow.ToString("dd MMM yyyy"),
                ["reason"]    = message.Reason,
            },
            MetadataJson = $"{{\"kitId\":{message.KitId}}}",
        }, ct);

        _logger.LogInformation("Notification sent for CargoDryKitRevoked: {KitCode}", message.KitCode);
    }

    public override Task ExecuteRollbackMessage(CargoDryKitRevokedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: CargoDryKitRevokedConsumer for {KitCode}: {Error}", message.KitCode, ex.Message);
        return Task.CompletedTask;
    }
}
