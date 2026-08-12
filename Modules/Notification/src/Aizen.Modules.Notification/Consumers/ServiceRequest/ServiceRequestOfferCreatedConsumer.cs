using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
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
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly ILogger<ServiceRequestOfferCreatedConsumer> _logger;

    public ServiceRequestOfferCreatedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender   = sp.GetRequiredService<ISender>();
        _identity = sp.GetRequiredService<INotificationIdentityRemoteCall>();
        _logger   = sp.GetRequiredService<ILogger<ServiceRequestOfferCreatedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestOfferCreatedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestOfferCreatedMessage message, CancellationToken ct)
    {
        // Providers' notifications/tokens/preferences are keyed by ProviderProfileId (the id the provider inbox +
        // push pipeline resolve — see GetUserNotifications / N-A / N-B). Using the raw ProviderUserId would file the
        // notification where the provider never queries (invisible inbox row, no push). C2 recipient fix.
        // BE_NF2: InApp (+web/FCM push) + Email.
        var offerVariables = new Dictionary<string, string>
        {
            { "serviceRequestId", message.ServiceRequestId.ToString() },
            { "offerId",          message.OfferId.ToString() },
            { "totalAmount",      message.TotalAmount.ToString("F2") },
            { "currencyCode",     message.CurrencyCode },
        };
        var offerMetadata = $"{{\"serviceRequestId\":{message.ServiceRequestId},\"offerId\":{message.OfferId}}}";

        await NotificationChannelDispatch.SendInAppAndEmailAsync(
            _sender, message.ProviderProfileId, NotificationType.OfferCreated,
            offerVariables, offerMetadata, "ServiceRequest", message.ServiceRequestId, ct);

        // BE_NF1 (D2) — also notify the OWNER that they received an offer. Previously the owner got nothing; only the
        // provider was notified. Owner-facing OfferReceived type (distinct template). Skip if the owner id is absent
        // (legacy publisher) so we never file a notification to user 0. BE_NF1b: file under the owner's participant
        // PROFILE id (where the owner inbox + device tokens resolve), not the raw user id.
        if (message.OwnerUserId != 0)
        {
            var ownerRecipientId = await OwnerRecipientResolver.ResolveAsync(_identity, _logger, message.OwnerUserId, ct);
            await NotificationChannelDispatch.SendInAppAndEmailAsync(
                _sender, ownerRecipientId, NotificationType.OfferReceived,
                offerVariables, offerMetadata, "ServiceRequest", message.ServiceRequestId, ct);
        }
    }

    public override Task ExecuteRollbackMessage(ServiceRequestOfferCreatedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestOfferCreatedConsumer SR={SrId}: {Error}", message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
