using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.ServiceRequest;

/// <summary>
/// N3-C — the completion auto-approval deadline is approaching. Nudges the <b>owner</b> to review before the work is
/// auto-approved (<see cref="NotificationType.CompletionAutoApproveApproaching"/> 133, ServiceRequests category).
/// The job emits this once per completion.
/// </summary>
public sealed class ServiceRequestCompletionAutoApproveApproachingConsumer
    : AizenBaseMessageConsumer<ServiceRequestCompletionAutoApproveApproachingMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<ServiceRequestCompletionAutoApproveApproachingConsumer> _logger;

    public ServiceRequestCompletionAutoApproveApproachingConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<ServiceRequestCompletionAutoApproveApproachingConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestCompletionAutoApproveApproachingMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestCompletionAutoApproveApproachingMessage message, CancellationToken ct)
    {
        if (message.OwnerUserId == 0) return;

        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.OwnerUserId,
            Type            = NotificationType.CompletionAutoApproveApproaching,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                { "serviceRequestId", message.ServiceRequestId.ToString() },
                { "completionId",     message.CompletionId.ToString() },
                { "daysRemaining",    message.DaysRemaining.ToString() },
            },
            MetadataJson  = $"{{\"serviceRequestId\":{message.ServiceRequestId},\"completionId\":{message.CompletionId}}}",
            ReferenceType = "ServiceRequest",
            ReferenceId   = message.ServiceRequestId,
        }, ct);

        _logger.LogInformation(
            "CompletionAutoApproveApproaching SR={SrId} Completion={CompletionId} ({Days}d) → notified owner {OwnerId}.",
            message.ServiceRequestId, message.CompletionId, message.DaysRemaining, message.OwnerUserId);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestCompletionAutoApproveApproachingMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestCompletionAutoApproveApproachingConsumer SR={SrId}: {Error}", message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
