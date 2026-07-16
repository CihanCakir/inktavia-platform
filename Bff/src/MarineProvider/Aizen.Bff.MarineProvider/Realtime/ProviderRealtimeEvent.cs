namespace Aizen.Bff.MarineProvider.Realtime;

/// <summary>
/// What the SPA receives on the "providerEvent" channel.
///
/// Deliberately thin: an event type, the ids needed to refetch, and just enough text to render a toast. The client
/// invalidates its cache and asks the API for the truth — a realtime frame is a hint that something changed, never
/// the source of record. Nothing sensitive travels here.
/// </summary>
public sealed class ProviderRealtimeEvent
{
    public string EventType { get; init; } = default!;
    public long ServiceRequestId { get; init; }
    public string? RequestCode { get; init; }
    public string? Title { get; init; }
    public string? CityCode { get; init; }
    public string? MarinaName { get; init; }
    public long? OfferId { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public static class ProviderRealtimeEventTypes
{
    /// <summary>A request became biddable in a city this provider works in.</summary>
    public const string ServiceRequestPublished = "ServiceRequestPublished";

    /// <summary>This provider's offer was accepted. The one they actually care about.</summary>
    public const string OfferAccepted = "OfferAccepted";

    /// <summary>A service request's details (title, description, dates, etc.) were updated.</summary>
    public const string ServiceRequestUpdated = "ServiceRequestUpdated";

    /// <summary>A service request was cancelled by its owner.</summary>
    public const string ServiceRequestCancelled = "ServiceRequestCancelled";

    /// <summary>A service request's priority was changed.</summary>
    public const string ServiceRequestUrgencyChanged = "ServiceRequestUrgencyChanged";

    /// <summary>This provider's offer was rejected by the owner.</summary>
    public const string OfferRejected = "OfferRejected";

    /// <summary>This provider's offer was viewed by the customer.</summary>
    public const string OfferViewedByCustomer = "OfferViewedByCustomer";

    /// <summary>The customer requested a revision on this provider's offer.</summary>
    public const string OfferRevisionRequested = "OfferRevisionRequested";
}
