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
/// N3-C — a completion was approved (owner-approval path — manual OR the auto-approval job). Notifies the
/// <b>provider</b> that their work was approved and payout is imminent (<see cref="NotificationType.CompletionApproved"/>
/// 131, ServiceRequests category). Fires from the same <c>ServiceRequestCompletionApprovedMessage</c> either way.
/// </summary>
public sealed class ServiceRequestCompletionApprovedConsumer
    : AizenBaseMessageConsumer<ServiceRequestCompletionApprovedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<ServiceRequestCompletionApprovedConsumer> _logger;

    public ServiceRequestCompletionApprovedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<ServiceRequestCompletionApprovedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestCompletionApprovedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestCompletionApprovedMessage message, CancellationToken ct)
    {
        if (message.ProviderUserId == 0) return;

        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.ProviderUserId,
            Type            = NotificationType.CompletionApproved,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                { "serviceRequestId", message.ServiceRequestId.ToString() },
                { "completionId",     message.CompletionId.ToString() },
            },
            MetadataJson  = $"{{\"serviceRequestId\":{message.ServiceRequestId},\"completionId\":{message.CompletionId}}}",
            ReferenceType = "ServiceRequest",
            ReferenceId   = message.ServiceRequestId,
        }, ct);

        _logger.LogInformation(
            "CompletionApproved SR={SrId} Completion={CompletionId} → notified provider {ProviderId}.",
            message.ServiceRequestId, message.CompletionId, message.ProviderUserId);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestCompletionApprovedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestCompletionApprovedConsumer SR={SrId}: {Error}", message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
