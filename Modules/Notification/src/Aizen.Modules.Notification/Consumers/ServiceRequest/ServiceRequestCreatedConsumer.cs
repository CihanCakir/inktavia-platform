using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.ServiceRequest;

public sealed class ServiceRequestCreatedConsumer
    : AizenBaseMessageConsumer<ServiceRequestCreatedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<ServiceRequestCreatedConsumer> _logger;

    public ServiceRequestCreatedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<ServiceRequestCreatedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestCreatedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestCreatedMessage message, CancellationToken ct)
    {
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.OwnerUserId,
            Type            = NotificationType.ServiceRequestCreated,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                { "requestCode", message.RequestCode },
                { "serviceName", message.ServiceCategoryCode },
            },
            MetadataJson = $"{{\"serviceRequestId\":{message.ServiceRequestId}}}",
        }, ct);

        _logger.LogInformation("Notification sent for ServiceRequestCreated: {RequestCode}", message.RequestCode);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestCreatedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestCreatedConsumer for {RequestCode}: {Error}", message.RequestCode, ex.Message);
        return Task.CompletedTask;
    }
}
