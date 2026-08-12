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

/// <summary>
/// BE_NF1 (D1) — the region fan-out now fires on <see cref="ServiceRequestPublishedMessage"/>, the event that actually
/// publishes. It previously lived in ServiceRequestCreatedConsumer, bound to ServiceRequestCreatedMessage — a message
/// nothing ever published — so the fan-out never ran (0 provider notifications, 0 web push). The dead consumer has been
/// retired; this consumer replaces it.
/// </summary>
public sealed class ServiceRequestPublishedConsumer
    : AizenBaseMessageConsumer<ServiceRequestPublishedMessage>
{
    private readonly ISender _sender;
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly ILogger<ServiceRequestPublishedConsumer> _logger;

    public ServiceRequestPublishedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender   = sp.GetRequiredService<ISender>();
        _identity = sp.GetRequiredService<INotificationIdentityRemoteCall>();
        _logger   = sp.GetRequiredService<ILogger<ServiceRequestPublishedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestPublishedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestPublishedMessage message, CancellationToken ct)
    {
        // 1) The owner's "your request is now live / open for offers" confirmation. Supersedes the never-fired
        //    ServiceRequestCreated confirmation (whose event was never published). BE_NF1b: file under the owner's
        //    participant PROFILE id (where the owner inbox + device tokens resolve), not the raw user id.
        var ownerRecipientId = await OwnerRecipientResolver.ResolveAsync(_identity, _logger, message.OwnerUserId, ct);
        await NotificationChannelDispatch.SendInAppAndEmailAsync(   // BE_NF2: InApp (+web/FCM push) + Email
            _sender, ownerRecipientId, NotificationType.ServiceRequestPublished,
            new Dictionary<string, string>
            {
                { "requestCode", message.RequestCode },
                { "serviceName", message.ServiceCategoryCode },
            },
            $"{{\"serviceRequestId\":{message.ServiceRequestId}}}",
            "ServiceRequest", message.ServiceRequestId, ct);

        _logger.LogInformation("Notification sent for ServiceRequestPublished: {RequestCode}", message.RequestCode);

        // 2) N-C region fan-out — notify providers operating in this city + category. Best-effort: a resolver failure
        //    (Identity down, etc.) must not roll back the owner notification, so it's isolated in its own try/catch.
        await FanOutToAreaProvidersAsync(message, ct);
    }

    private async Task FanOutToAreaProvidersAsync(ServiceRequestPublishedMessage message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.LocationCityCode))
        {
            _logger.LogDebug("SR {RequestCode} has no LocationCityCode; region fan-out skipped.", message.RequestCode);
            return;
        }

        List<ProviderForAreaResult> providers;
        try
        {
            var response = await _identity.GetProvidersForArea(message.LocationCityCode!, message.ServiceCategoryCode);
            providers = response.Body ?? new List<ProviderForAreaResult>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Region provider resolution failed for SR {RequestCode} (city={City}, category={Category}); " +
                "owner notified, area fan-out skipped.",
                message.RequestCode, message.LocationCityCode, message.ServiceCategoryCode);
            return;
        }

        // One notification per provider, SEQUENTIALLY (respects the WS2 / single-scoped-DbContext pattern). Exclude the
        // owner so the requester isn't double-notified. RecipientUserId = ProviderProfileId (where provider tokens/
        // prefs/inbox live). Each inherits N-B ServiceRequests gating (in-app always; push only if opted-in+subscribed).
        var sent = 0;
        foreach (var provider in providers)
        {
            if (provider.ProfileId == message.OwnerUserId) continue;

            await NotificationChannelDispatch.SendInAppAndEmailAsync(   // BE_NF2: InApp (+web/FCM push) + Email
                _sender, provider.ProfileId, NotificationType.ServiceRequestAreaOpportunity,
                new Dictionary<string, string>
                {
                    { "title",       message.Title },
                    { "requestCode", message.RequestCode },
                },
                $"{{\"serviceRequestId\":{message.ServiceRequestId}}}",
                "ServiceRequest", message.ServiceRequestId, ct);
            sent++;
        }

        _logger.LogInformation(
            "SR {RequestCode} region fan-out: notified {Count} providers in city {City} (category {Category}).",
            message.RequestCode, sent, message.LocationCityCode, message.ServiceCategoryCode);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestPublishedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestPublishedConsumer for {RequestCode}: {Error}", message.RequestCode, ex.Message);
        return Task.CompletedTask;
    }
}
