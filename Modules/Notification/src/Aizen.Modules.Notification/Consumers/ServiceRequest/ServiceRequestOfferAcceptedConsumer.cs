using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.ServiceRequest;

public sealed class ServiceRequestOfferAcceptedConsumer
    : AizenBaseMessageConsumer<ServiceRequestOfferAcceptedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<ServiceRequestOfferAcceptedConsumer> _logger;

    public ServiceRequestOfferAcceptedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<ServiceRequestOfferAcceptedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestOfferAcceptedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestOfferAcceptedMessage message, CancellationToken ct)
    {
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.OwnerUserId,
            Type            = NotificationType.OfferAccepted,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                { "serviceRequestId", message.ServiceRequestId.ToString() },
                { "offerId",          message.OfferId.ToString() },
            },
            MetadataJson = $"{{\"serviceRequestId\":{message.ServiceRequestId},\"offerId\":{message.OfferId}}}",
            ReferenceType = "ServiceRequest",
            ReferenceId   = message.ServiceRequestId,
        }, ct);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestOfferAcceptedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestOfferAcceptedConsumer SR={SrId}: {Error}", message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
