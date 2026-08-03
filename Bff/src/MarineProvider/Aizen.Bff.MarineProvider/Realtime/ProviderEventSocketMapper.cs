using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Abstraction.Models;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;

namespace Aizen.Bff.MarineProvider.Realtime;

/// <summary>
/// The single per-surface routing declaration (ADR layer-2): "this module bus event → this frame + these target
/// group(s)". Replaces the seven hand-rolled <c>*RealtimeConsumer</c> classes; the framework's
/// <c>RealtimeIngressService</c> resolves this one <see cref="IEventSocketMapper"/> and calls <see cref="Map"/> +
/// <see cref="GetTargets"/> for each consumed bus event.
///
/// Behaviour is preserved exactly against the deleted consumers — same EventType, same target group, same filters,
/// same <see cref="ProviderRealtimeEvent"/> payload shape (delivered to the SPA as <c>env.payload</c> on the
/// framework's single "ReceiveEvent" method, with Type "providerEvent").
///
/// SKIPPING (a filtered-out event) is done by returning <c>null</c> from <see cref="Map"/>. This is deliberate:
/// the ingress treats an empty <see cref="GetTargets"/> result as "broadcast to the message's Stream channel", NOT
/// as "skip". So a dropped event must be dropped at Map — never by returning empty targets. Map and GetTargets are
/// both driven by the single <see cref="Resolve"/> method so they can never disagree about whether an event fires
/// or which group it targets.
/// </summary>
public sealed class ProviderEventSocketMapper : IEventSocketMapper
{
    public RealtimeMessage? Map(object domainEvent) => Resolve(domainEvent)?.Message;

    public (IEnumerable<string> UserIds, IEnumerable<string> GroupNames) GetTargets(object domainEvent)
    {
        var resolved = Resolve(domainEvent);
        return resolved is null
            ? (Array.Empty<string>(), Array.Empty<string>())
            : (Array.Empty<string>(), new[] { resolved.Value.Group });
    }

    /// <summary>
    /// Port of every consumer's routing + filter into one place. Returns <c>null</c> when the event should not
    /// fire (the old consumer's early-return cases), otherwise the frame + the single target group.
    /// </summary>
    private static (RealtimeMessage Message, string Group)? Resolve(object domainEvent) => Unwrap(domainEvent) switch
    {
        // MessageAdded — notify the provider for Owner (counterparty) and System (lifecycle pill) messages only;
        // the provider's own sends must not self-notify. Skip when no provider profile id is carried.
        ServiceRequestMessageSentMessage m
            when (m.SenderType == ServiceRequestMessageSenderType.Owner
                  || m.SenderType == ServiceRequestMessageSenderType.System)
                 && m.ProviderProfileId is > 0
            => Frame(
                ProviderRealtimeHub.ProviderGroup(m.ProviderProfileId!.Value),
                m.ServiceRequestId.ToString(),
                new ProviderRealtimeEvent
                {
                    EventType = ProviderRealtimeEventTypes.MessageAdded,
                    ServiceRequestId = m.ServiceRequestId,
                    MessageSenderType = m.SenderType.ToString(),
                }),

        // OfferAccepted — addressed to exactly one provider group. Skip when no provider profile id.
        ServiceRequestOfferAcceptedMessage m when m.ProviderProfileId > 0
            => Frame(
                ProviderRealtimeHub.ProviderGroup(m.ProviderProfileId),
                m.OfferId.ToString(),
                new ProviderRealtimeEvent
                {
                    EventType = ProviderRealtimeEventTypes.OfferAccepted,
                    ServiceRequestId = m.ServiceRequestId,
                    OfferId = m.OfferId,
                }),

        // OfferRejected — addressed to exactly one provider group. Skip when no provider profile id.
        ServiceRequestOfferRejectedMessage m when m.ProviderProfileId > 0
            => Frame(
                ProviderRealtimeHub.ProviderGroup(m.ProviderProfileId),
                m.OfferId.ToString(),
                new ProviderRealtimeEvent
                {
                    EventType = ProviderRealtimeEventTypes.OfferRejected,
                    ServiceRequestId = m.ServiceRequestId,
                    OfferId = m.OfferId,
                }),

        // ServiceRequestPublished — fan out to the city group. Skip when the request carries no city.
        ServiceRequestPublishedMessage m when !string.IsNullOrWhiteSpace(m.LocationCityCode)
            => Frame(
                ProviderRealtimeHub.CityGroup(m.LocationCityCode!),
                m.ServiceRequestId.ToString(),
                new ProviderRealtimeEvent
                {
                    EventType = ProviderRealtimeEventTypes.ServiceRequestPublished,
                    ServiceRequestId = m.ServiceRequestId,
                    RequestCode = m.RequestCode,
                    Title = m.Title,
                    CityCode = m.LocationCityCode,
                    MarinaName = m.LocationMarinaName,
                    OccurredAt = m.PublishedAt,
                }),

        // ServiceRequestUpdated — city group. Skip when no city.
        ServiceRequestUpdatedMessage m when !string.IsNullOrWhiteSpace(m.LocationCityCode)
            => Frame(
                ProviderRealtimeHub.CityGroup(m.LocationCityCode!),
                m.ServiceRequestId.ToString(),
                new ProviderRealtimeEvent
                {
                    EventType = ProviderRealtimeEventTypes.ServiceRequestUpdated,
                    ServiceRequestId = m.ServiceRequestId,
                    RequestCode = m.RequestCode,
                    CityCode = m.LocationCityCode,
                }),

        // ServiceRequestCancelled — city group. Skip when no city.
        ServiceRequestCancelledMessage m when !string.IsNullOrWhiteSpace(m.LocationCityCode)
            => Frame(
                ProviderRealtimeHub.CityGroup(m.LocationCityCode!),
                m.ServiceRequestId.ToString(),
                new ProviderRealtimeEvent
                {
                    EventType = ProviderRealtimeEventTypes.ServiceRequestCancelled,
                    ServiceRequestId = m.ServiceRequestId,
                    RequestCode = m.RequestCode,
                    CityCode = m.LocationCityCode,
                }),

        // ServiceRequestUrgencyChanged — city group. Skip when no city.
        ServiceRequestUrgencyChangedMessage m when !string.IsNullOrWhiteSpace(m.LocationCityCode)
            => Frame(
                ProviderRealtimeHub.CityGroup(m.LocationCityCode!),
                m.ServiceRequestId.ToString(),
                new ProviderRealtimeEvent
                {
                    EventType = ProviderRealtimeEventTypes.ServiceRequestUrgencyChanged,
                    ServiceRequestId = m.ServiceRequestId,
                    RequestCode = m.RequestCode,
                    CityCode = m.LocationCityCode,
                }),

        _ => null,
    };

    private static (RealtimeMessage Message, string Group) Frame(string group, string aggregateId, ProviderRealtimeEvent payload)
        => (new RealtimeMessage
        {
            Type = "providerEvent",
            Stream = group,
            AggregateId = aggregateId,
            Payload = payload,
        }, group);

    // The generic RealtimeEventConsumer wraps the bus message in an EventDto (Data = the message); accept both the
    // wrapped and the raw form so the mapper is robust to how it is invoked (mirrors the admin mapper).
    private static object? Unwrap(object domainEvent) => domainEvent switch
    {
        EventDto { Data: { } data } => data,
        _ => domainEvent,
    };
}
