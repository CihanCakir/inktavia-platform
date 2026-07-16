namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;

public sealed class ProviderDiscoverySummaryResponse
{
    public int OpenCount { get; init; }
    public int PublishedTodayCount { get; init; }
    public int EmergencyCount { get; init; }
    public int MyActiveOfferCount { get; init; }
    public string LocationMode { get; init; } = "City";
}
