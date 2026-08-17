using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.ServiceRequest;

public sealed class ServiceRequestStatusChangedConsumer
    : AizenBaseMessageConsumer<ServiceRequestStatusChangedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<ServiceRequestStatusChangedConsumer> _logger;

    public ServiceRequestStatusChangedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<ServiceRequestStatusChangedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestStatusChangedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestStatusChangedMessage message, CancellationToken ct)
    {
        var recipientUserId = message.ActorUserId ?? 0;
        if (recipientUserId == 0)
        {
            _logger.LogWarning("StatusChanged notification skipped — no ActorUserId for {Code}", message.RequestCode);
            return;
        }

        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = recipientUserId,
            Type            = NotificationType.ServiceRequestStatusChanged,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                { "requestCode", message.RequestCode },
                { "fromStatus",  message.FromStatus.ToString() },
                { "toStatus",    message.ToStatus.ToString() },
            },
            MetadataJson = $"{{\"serviceRequestId\":{message.ServiceRequestId}}}",
        }, ct);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestStatusChangedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestStatusChangedConsumer {Code}: {Error}", message.RequestCode, ex.Message);
        return Task.CompletedTask;
    }
}
