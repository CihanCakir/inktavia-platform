using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.ServiceRequest;

/// <summary>
/// N3-A (targeting fix) — a dispute was opened. Notifies <b>admin + both parties</b> (owner + provider), so the
/// counterparty is always covered regardless of who opened it (previously only the opener was notified). Recipients
/// are de-duplicated and 0/unknown ids skipped. Admin ids come from Identity (best-effort; a failure still notifies
/// the parties). DisputeOpened → Disputes category (N-B gated).
/// </summary>
public sealed class ServiceRequestDisputeOpenedConsumer
    : AizenBaseMessageConsumer<ServiceRequestDisputeOpenedMessage>
{
    private readonly ISender _sender;
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly ILogger<ServiceRequestDisputeOpenedConsumer> _logger;

    public ServiceRequestDisputeOpenedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender   = sp.GetRequiredService<ISender>();
        _identity = sp.GetRequiredService<INotificationIdentityRemoteCall>();
        _logger   = sp.GetRequiredService<ILogger<ServiceRequestDisputeOpenedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestDisputeOpenedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestDisputeOpenedMessage message, CancellationToken ct)
    {
        // Both parties (owner + provider). 0 = unknown (e.g. no accepted offer) → skipped.
        var recipients = new HashSet<long>();
        if (message.OwnerUserId != 0) recipients.Add(message.OwnerUserId);
        else
            // Surface the silent owner skip (the class of gap that hid the offer bug for a month): no owner id on the
            // message, so the owner will NOT be told a dispute was opened.
            _logger.LogWarning(
                "Owner dispute-opened notification SKIPPED: message carried no owner id (OwnerUserId=0). SR={SrId} Dispute={DisputeId}.",
                message.ServiceRequestId, message.DisputeId);
        if (message.ProviderUserId != 0) recipients.Add(message.ProviderUserId);

        // Admins (best-effort — a failure must not drop the party notifications).
        try
        {
            var admins = (await _identity.GetAdminUserIds()).Body ?? new List<long>();
            foreach (var adminId in admins) if (adminId != 0) recipients.Add(adminId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "DisputeOpened SR={SrId}: could not resolve admin ids; parties still notified.", message.ServiceRequestId);
        }

        foreach (var userId in recipients)
        {
            await _sender.Send(new SendNotificationCommand
            {
                RecipientUserId = userId,
                Type            = NotificationType.DisputeOpened,
                Channel         = NotificationChannel.InApp,
                Variables = new Dictionary<string, string>
                {
                    { "serviceRequestId", message.ServiceRequestId.ToString() },
                    { "disputeId",        message.DisputeId.ToString() },
                    { "reason",           message.Reason.ToString() },
                },
                MetadataJson = $"{{\"serviceRequestId\":{message.ServiceRequestId},\"disputeId\":{message.DisputeId}}}",
                ReferenceType = "ServiceRequest",
                ReferenceId   = message.ServiceRequestId,
            }, ct);
        }

        _logger.LogInformation(
            "DisputeOpened SR={SrId} Dispute={DisputeId} → notified {Count} recipients (owner+provider+admins).",
            message.ServiceRequestId, message.DisputeId, recipients.Count);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestDisputeOpenedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestDisputeOpenedConsumer SR={SrId}: {Error}", message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
