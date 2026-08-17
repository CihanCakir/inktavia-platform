using System.Globalization;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.ServiceRequest;

/// <summary>
/// N2 — reminds the vessel owner that a recurring maintenance schedule (S12) is due soon, routed through the N-B
/// path (InApp baseline write; push/email honour the ServiceRequests-category preferences). Requires the seeded
/// <c>SR_MAINTENANCE_REMINDER_DUE_INAPP</c> template (a type without a template is a silent no-op).
/// </summary>
public sealed class MaintenanceReminderDueConsumer
    : AizenBaseMessageConsumer<MaintenanceReminderDueMessage>
{
    private readonly ISender _sender;
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly ILogger<MaintenanceReminderDueConsumer> _logger;

    public MaintenanceReminderDueConsumer(IServiceProvider sp) : base(sp)
    {
        _sender   = sp.GetRequiredService<ISender>();
        _identity = sp.GetRequiredService<INotificationIdentityRemoteCall>();
        _logger   = sp.GetRequiredService<ILogger<MaintenanceReminderDueConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(MaintenanceReminderDueMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(MaintenanceReminderDueMessage message, CancellationToken ct)
    {
        if (message.OwnerUserId == 0) return;

        var vessel = string.IsNullOrWhiteSpace(message.VesselName) ? $"#{message.VesselId}" : message.VesselName!;
        var date   = message.NextDueAt.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

        // BE_NF3 — owner-facing. Resolve to the participant profile id (NF1b) + deliver multi-channel.
        var ownerRecipientId = await OwnerRecipientResolver.ResolveAsync(_identity, _logger, message.OwnerUserId, ct);
        await NotificationChannelDispatch.SendInAppAndEmailAsync(
            _sender, ownerRecipientId, NotificationType.MaintenanceReminderDue,
            new Dictionary<string, string>
            {
                { "vessel",   vessel },
                { "category", message.ServiceCategoryCode },
                { "date",     date },
            },
            $"{{\"scheduleId\":{message.ScheduleId},\"vesselId\":{message.VesselId}}}",
            "MaintenanceSchedule", message.ScheduleId, ct);

        _logger.LogInformation(
            "MaintenanceReminderDue schedule={ScheduleId} vessel={VesselId} category={Category} → owner {OwnerId} notified.",
            message.ScheduleId, message.VesselId, message.ServiceCategoryCode, message.OwnerUserId);
    }

    public override Task ExecuteRollbackMessage(
        MaintenanceReminderDueMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning(
            "Rollback: MaintenanceReminderDueConsumer schedule={ScheduleId}: {Error}", message.ScheduleId, ex.Message);
        return Task.CompletedTask;
    }
}
