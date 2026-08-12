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
        // BE_NF3 — OfferAccepted is a PROVIDER-facing event ("your offer was accepted"). It previously mis-notified the
        // OWNER by the raw OwnerUserId, InApp only. Notify the provider by ProviderProfileId (where the provider inbox +
        // push resolve), multi-channel (InApp + web/FCM push + email). (An optional owner "you accepted an offer"
        // confirmation is deferred — the meaningful notification is the provider's.)
        if (message.ProviderProfileId == 0) return;

        await NotificationChannelDispatch.SendInAppAndEmailAsync(
            _sender, message.ProviderProfileId, NotificationType.OfferAccepted,
            new Dictionary<string, string>
            {
                { "serviceRequestId", message.ServiceRequestId.ToString() },
                { "offerId",          message.OfferId.ToString() },
            },
            $"{{\"serviceRequestId\":{message.ServiceRequestId},\"offerId\":{message.OfferId}}}",
            "ServiceRequest", message.ServiceRequestId, ct);

        _logger.LogInformation(
            "OfferAccepted SR={SrId} Offer={OfferId} → notified provider {ProviderProfileId}.",
            message.ServiceRequestId, message.OfferId, message.ProviderProfileId);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestOfferAcceptedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestOfferAcceptedConsumer SR={SrId}: {Error}", message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
