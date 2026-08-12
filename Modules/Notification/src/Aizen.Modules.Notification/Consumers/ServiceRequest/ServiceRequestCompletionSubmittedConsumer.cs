using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.ServiceRequest;

public sealed class ServiceRequestCompletionSubmittedConsumer
    : AizenBaseMessageConsumer<ServiceRequestCompletionSubmittedMessage>
{
    private readonly ISender _sender;
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly ILogger<ServiceRequestCompletionSubmittedConsumer> _logger;

    public ServiceRequestCompletionSubmittedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender   = sp.GetRequiredService<ISender>();
        _identity = sp.GetRequiredService<INotificationIdentityRemoteCall>();
        _logger   = sp.GetRequiredService<ILogger<ServiceRequestCompletionSubmittedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestCompletionSubmittedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestCompletionSubmittedMessage message, CancellationToken ct)
    {
        if (message.OwnerUserId == 0) return;

        // BE_NF3 — owner-facing. Was filed under the raw OwnerUserId (invisible to the owner inbox per NF1b), InApp only.
        // Resolve to the participant profile id + deliver multi-channel (InApp + web/FCM push + email).
        var ownerRecipientId = await OwnerRecipientResolver.ResolveAsync(_identity, _logger, message.OwnerUserId, ct);
        await NotificationChannelDispatch.SendInAppAndEmailAsync(
            _sender, ownerRecipientId, NotificationType.CompletionSubmitted,
            new Dictionary<string, string>
            {
                { "serviceRequestId", message.ServiceRequestId.ToString() },
                { "completionId",     message.CompletionId.ToString() },
            },
            $"{{\"serviceRequestId\":{message.ServiceRequestId},\"completionId\":{message.CompletionId}}}",
            "ServiceRequest", message.ServiceRequestId, ct);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestCompletionSubmittedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestCompletionSubmittedConsumer SR={SrId}: {Error}", message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
