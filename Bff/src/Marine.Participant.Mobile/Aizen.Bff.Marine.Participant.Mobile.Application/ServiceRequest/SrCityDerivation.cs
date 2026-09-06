namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// Decision for deriving an SR's LocationCityCode from coordinates (custom map pin / bare coords) via the nearest
/// marina — closing the fan-out gap where such SRs carried lat/lng but no city, so the city:{code} SignalR fan-out +
/// provider city web-push skipped them. Pure + testable: the nearby lookup lives in the handler; the RULE lives here.
/// </summary>
public static class SrCityDerivation
{
    /// <summary>Sanity cap on how far the nearest marina may be to be a trustworthy city proxy. Turkey's coastal
    /// geography (a marina is near its province) makes 75 km safe; beyond it, don't guess.</summary>
    public const double MaxDeriveDistanceMeters = 75_000;

    /// <summary>
    /// Returns the cityCode to use. NEVER overrides an existing (supplied or marina-derived) city — only fills when
    /// it's null. Fills from the nearest marina's cityCode only when that marina is within
    /// <see cref="MaxDeriveDistanceMeters"/> and actually has a cityCode; otherwise the current (null) value stands.
    /// </summary>
    public static string? ResolveCityCode(string? currentCityCode, string? nearestCityCode, double? nearestDistanceMeters)
    {
        if (!string.IsNullOrWhiteSpace(currentCityCode)) return currentCityCode;              // never override
        if (string.IsNullOrWhiteSpace(nearestCityCode)) return currentCityCode;               // no proxy → stays null
        if (nearestDistanceMeters is null || nearestDistanceMeters > MaxDeriveDistanceMeters)
            return currentCityCode;                                                            // beyond cap → stays null
        return nearestCityCode;
    }
}
