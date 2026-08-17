using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.ServiceRequest;

public sealed class ServiceRequestDisputeOpenedConsumer
    : AizenBaseMessageConsumer<ServiceRequestDisputeOpenedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<ServiceRequestDisputeOpenedConsumer> _logger;

    public ServiceRequestDisputeOpenedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<ServiceRequestDisputeOpenedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestDisputeOpenedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestDisputeOpenedMessage message, CancellationToken ct)
    {
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.OpenedByUserId,
            Type            = NotificationType.DisputeOpened,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                { "serviceRequestId", message.ServiceRequestId.ToString() },
                { "disputeId",        message.DisputeId.ToString() },
                { "reason",           message.Reason.ToString() },
            },
            MetadataJson = $"{{\"serviceRequestId\":{message.ServiceRequestId},\"disputeId\":{message.DisputeId}}}",
        }, ct);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestDisputeOpenedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestDisputeOpenedConsumer SR={SrId}: {Error}", message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
