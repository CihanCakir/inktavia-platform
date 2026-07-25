using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.CargoDry;

public sealed class CargoDryProviderMilestoneReachedConsumer
    : AizenBaseMessageConsumer<CargoDryProviderMilestoneReachedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<CargoDryProviderMilestoneReachedConsumer> _logger;

    public CargoDryProviderMilestoneReachedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<CargoDryProviderMilestoneReachedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(CargoDryProviderMilestoneReachedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(CargoDryProviderMilestoneReachedMessage message, CancellationToken ct)
    {
        var notificationType = message.MilestoneType switch
        {
            "FirstSale"             => NotificationType.CargoDryProviderFirstSale,
            "MonthlyTargetReached"  => NotificationType.CargoDryProviderMonthlyTargetReached,
            "TierUp"                => NotificationType.CargoDryProviderTierUp,
            "StreakMilestone"        => NotificationType.CargoDryProviderStreakMilestone,
            _ => NotificationType.CargoDryProviderFirstSale,
        };

        var variables = new Dictionary<string, string>
        {
            ["displayValue"]  = message.DisplayValue ?? "",
            ["milestoneType"] = message.MilestoneType,
            ["periodKey"]     = message.PeriodKey,
        };
        var metadataJson = $"{{\"milestoneType\":\"{message.MilestoneType}\",\"periodKey\":\"{message.PeriodKey}\"}}";

        // InApp notification (existing)
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.ProviderProfileId,
            Type            = notificationType,
            Channel         = NotificationChannel.InApp,
            Variables       = variables,
            MetadataJson    = metadataJson,
            ReferenceType   = "Milestone",
            ReferenceId     = null,
        }, ct);

        // Web Push notification (CE-6c-(b) — additive; safe no-op when no subscription)
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.ProviderProfileId,
            Type            = notificationType,
            Channel         = NotificationChannel.Push,
            Variables       = variables,
            MetadataJson    = metadataJson,
            ReferenceType   = "Milestone",
            ReferenceId     = null,
        }, ct);

        _logger.LogInformation(
            "Milestone notification sent (InApp + Push): {Type}/{PeriodKey} for provider {Pid}.",
            message.MilestoneType, message.PeriodKey, message.ProviderProfileId);
    }

    public override Task ExecuteRollbackMessage(CargoDryProviderMilestoneReachedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning(
            "Rollback: CargoDryProviderMilestoneReachedConsumer for {Type}/{PeriodKey}: {Error}",
            message.MilestoneType, message.PeriodKey, ex.Message);
        return Task.CompletedTask;
    }
}
