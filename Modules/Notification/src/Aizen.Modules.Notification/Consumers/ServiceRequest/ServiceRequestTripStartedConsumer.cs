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
/// Phase-2 — TRIP_STARTED. The provider marked "en route" for an accepted job → notify the OWNER
/// ("Sağlayıcı yola çıktı") so an owner not on the map screen still learns. Clones the JOB_STARTED path
/// (<see cref="ServiceRequestAssignmentStartedConsumer"/>): owner-facing, keyed by the participant PROFILE id via
/// <see cref="OwnerRecipientResolver"/>, multi-channel (InApp + web/FCM push + email) and preference-gated.
/// </summary>
public sealed class ServiceRequestTripStartedConsumer
    : AizenBaseMessageConsumer<ServiceRequestTripStartedMessage>
{
    private readonly ISender _sender;
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly ILogger<ServiceRequestTripStartedConsumer> _logger;

    public ServiceRequestTripStartedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender   = sp.GetRequiredService<ISender>();
        _identity = sp.GetRequiredService<INotificationIdentityRemoteCall>();
        _logger   = sp.GetRequiredService<ILogger<ServiceRequestTripStartedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestTripStartedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestTripStartedMessage message, CancellationToken ct)
    {
        if (message.OwnerUserId == 0) return;

        var ownerRecipientId = await OwnerRecipientResolver.ResolveAsync(_identity, _logger, message.OwnerUserId, ct);
        await NotificationChannelDispatch.SendInAppAndEmailAsync(
            _sender, ownerRecipientId, NotificationType.TripStarted,
            new Dictionary<string, string>
            {
                { "serviceRequestId", message.ServiceRequestId.ToString() },
                { "requestCode",      message.RequestCode },
            },
            $"{{\"serviceRequestId\":{message.ServiceRequestId}}}",
            "ServiceRequest", message.ServiceRequestId, ct);

        _logger.LogInformation(
            "TripStarted SR={SrId} → notified owner {OwnerRecipientId}.",
            message.ServiceRequestId, ownerRecipientId);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestTripStartedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestTripStartedConsumer SR={SrId}: {Error}", message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
