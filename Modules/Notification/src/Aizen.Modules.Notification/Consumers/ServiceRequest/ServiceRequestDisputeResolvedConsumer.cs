using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.ServiceRequest;

/// <summary>
/// N3-A — a dispute was resolved (S13 <see cref="ServiceRequestDisputeResolvedMessage"/>). Notifies <b>both</b> the
/// owner and the provider with a <see cref="NotificationType.DisputeResolved"/> (141) message reflecting the outcome
/// (and refund amount when there was one). Disputes category → N-B gated per user.
/// </summary>
public sealed class ServiceRequestDisputeResolvedConsumer
    : AizenBaseMessageConsumer<ServiceRequestDisputeResolvedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<ServiceRequestDisputeResolvedConsumer> _logger;

    public ServiceRequestDisputeResolvedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<ServiceRequestDisputeResolvedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestDisputeResolvedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestDisputeResolvedMessage message, CancellationToken ct)
    {
        var outcomeLabel = message.Outcome == 0
            ? "Karar"
            : ((DisputeResolutionOutcome)message.Outcome).ToString();
        var refundSuffix = message.RefundAmount > 0m ? $" — {message.RefundAmount:F2}" : string.Empty;

        var recipients = new HashSet<long>();
        if (message.OwnerUserId != 0)    recipients.Add(message.OwnerUserId);
        if (message.ProviderUserId != 0) recipients.Add(message.ProviderUserId);

        foreach (var userId in recipients)
        {
            await _sender.Send(new SendNotificationCommand
            {
                RecipientUserId = userId,
                Type            = NotificationType.DisputeResolved,
                Channel         = NotificationChannel.InApp,
                Variables = new Dictionary<string, string>
                {
                    { "serviceRequestId", message.ServiceRequestId.ToString() },
                    { "disputeId",        message.DisputeId.ToString() },
                    { "outcome",          outcomeLabel },
                    { "refundAmount",     message.RefundAmount.ToString("F2") },
                    { "refundSuffix",     refundSuffix },
                },
                MetadataJson  = $"{{\"serviceRequestId\":{message.ServiceRequestId},\"disputeId\":{message.DisputeId}}}",
                ReferenceType = "ServiceRequest",
                ReferenceId   = message.ServiceRequestId,
            }, ct);
        }

        _logger.LogInformation(
            "DisputeResolved SR={SrId} Dispute={DisputeId} Outcome={Outcome} → notified {Count} parties.",
            message.ServiceRequestId, message.DisputeId, outcomeLabel, recipients.Count);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestDisputeResolvedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestDisputeResolvedConsumer SR={SrId}: {Error}", message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
