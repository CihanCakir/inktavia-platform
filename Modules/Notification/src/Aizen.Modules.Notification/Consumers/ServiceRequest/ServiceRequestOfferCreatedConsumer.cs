using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.ServiceRequest;

public sealed class ServiceRequestOfferCreatedConsumer
    : AizenBaseMessageConsumer<ServiceRequestOfferCreatedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<ServiceRequestOfferCreatedConsumer> _logger;

    public ServiceRequestOfferCreatedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<ServiceRequestOfferCreatedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestOfferCreatedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestOfferCreatedMessage message, CancellationToken ct)
    {
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.ProviderUserId,
            Type            = NotificationType.OfferCreated,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                { "serviceRequestId", message.ServiceRequestId.ToString() },
                { "offerId",          message.OfferId.ToString() },
                { "totalAmount",      message.TotalAmount.ToString("F2") },
                { "currencyCode",     message.CurrencyCode },
            },
            MetadataJson = $"{{\"serviceRequestId\":{message.ServiceRequestId},\"offerId\":{message.OfferId}}}",
        }, ct);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestOfferCreatedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestOfferCreatedConsumer SR={SrId}: {Error}", message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
