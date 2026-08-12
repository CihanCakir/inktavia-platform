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
/// BE_NF3 — JOB_STARTED. The provider started the assigned job → notify the OWNER ("the provider started your job").
/// The event (<see cref="ServiceRequestAssignmentStartedMessage"/>) was previously consumed only by Messaging (WC1
/// System message); Notification never told the owner. Owner-facing → keyed by the participant PROFILE id (NF1b
/// resolver), multi-channel (InApp + web/FCM push + email), preference-gated.
/// </summary>
public sealed class ServiceRequestAssignmentStartedConsumer
    : AizenBaseMessageConsumer<ServiceRequestAssignmentStartedMessage>
{
    private readonly ISender _sender;
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly ILogger<ServiceRequestAssignmentStartedConsumer> _logger;

    public ServiceRequestAssignmentStartedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender   = sp.GetRequiredService<ISender>();
        _identity = sp.GetRequiredService<INotificationIdentityRemoteCall>();
        _logger   = sp.GetRequiredService<ILogger<ServiceRequestAssignmentStartedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestAssignmentStartedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestAssignmentStartedMessage message, CancellationToken ct)
    {
        if (message.OwnerUserId == 0) return;

        var ownerRecipientId = await OwnerRecipientResolver.ResolveAsync(_identity, _logger, message.OwnerUserId, ct);
        await NotificationChannelDispatch.SendInAppAndEmailAsync(
            _sender, ownerRecipientId, NotificationType.JobStarted,
            new Dictionary<string, string>
            {
                { "serviceRequestId", message.ServiceRequestId.ToString() },
                { "requestCode",      message.RequestCode },
            },
            $"{{\"serviceRequestId\":{message.ServiceRequestId},\"assignmentId\":{message.AssignmentId}}}",
            "ServiceRequest", message.ServiceRequestId, ct);

        _logger.LogInformation(
            "JobStarted SR={SrId} Assignment={AssignmentId} → notified owner {OwnerRecipientId}.",
            message.ServiceRequestId, message.AssignmentId, ownerRecipientId);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestAssignmentStartedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestAssignmentStartedConsumer SR={SrId}: {Error}", message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
