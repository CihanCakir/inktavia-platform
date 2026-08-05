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

public sealed class ServiceRequestCreatedConsumer
    : AizenBaseMessageConsumer<ServiceRequestCreatedMessage>
{
    private readonly ISender _sender;
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly ILogger<ServiceRequestCreatedConsumer> _logger;

    public ServiceRequestCreatedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender   = sp.GetRequiredService<ISender>();
        _identity = sp.GetRequiredService<INotificationIdentityRemoteCall>();
        _logger   = sp.GetRequiredService<ILogger<ServiceRequestCreatedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestCreatedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestCreatedMessage message, CancellationToken ct)
    {
        // 1) The requester's own confirmation (unchanged).
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
            ReferenceType = "ServiceRequest",
            ReferenceId   = message.ServiceRequestId,
        }, ct);

        _logger.LogInformation("Notification sent for ServiceRequestCreated: {RequestCode}", message.RequestCode);

        // 2) N-C region fan-out — notify providers operating in this city + category. Best-effort: a resolver failure
        //    (Identity down, etc.) must not roll back the owner notification, so it's isolated in its own try/catch.
        await FanOutToAreaProvidersAsync(message, ct);
    }

    private async Task FanOutToAreaProvidersAsync(ServiceRequestCreatedMessage message, CancellationToken ct)
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

            await _sender.Send(new SendNotificationCommand
            {
                RecipientUserId = provider.ProfileId,
                Type            = NotificationType.ServiceRequestAreaOpportunity,
                Channel         = NotificationChannel.InApp,
                Variables = new Dictionary<string, string>
                {
                    { "title",       message.Title },
                    { "requestCode", message.RequestCode },
                },
                MetadataJson  = $"{{\"serviceRequestId\":{message.ServiceRequestId}}}",
                ReferenceType = "ServiceRequest",
                ReferenceId   = message.ServiceRequestId,
            }, ct);
            sent++;
        }

        _logger.LogInformation(
            "SR {RequestCode} region fan-out: notified {Count} providers in city {City} (category {Category}).",
            message.RequestCode, sent, message.LocationCityCode, message.ServiceCategoryCode);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestCreatedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestCreatedConsumer for {RequestCode}: {Error}", message.RequestCode, ex.Message);
        return Task.CompletedTask;
    }
}
