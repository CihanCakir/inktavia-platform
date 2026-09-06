using Aizen.Modules.ServiceRequest.Application.Query.Provider.GetProviderDiscovery;
using Aizen.Modules.ServiceRequest.Domain.Entities.Trip;

namespace Aizen.Modules.ServiceRequest.Application.Services.Trip;

/// <summary>
/// Straight-line trip math (no routing engine). ETA is deliberately coarse + labelled approximate; summary is a plain
/// great-circle sum of the trail. All methods are null-safe — insufficient data yields null.
/// </summary>
public static class TripMath
{
    /// <summary>Server-side ping throttle window: pings closer than this to the last one are ignored.</summary>
    public static readonly TimeSpan MinPingInterval = TimeSpan.FromSeconds(3);

    /// <summary>True when a new ping at <paramref name="now"/> arrives sooner than <see cref="MinPingInterval"/>
    /// after the last recorded ping (→ drop it).</summary>
    public static bool ShouldThrottle(DateTime? lastPingAt, DateTime now)
        => lastPingAt.HasValue && (now - lastPingAt.Value) < MinPingInterval;

    /// <summary>Coarse ETA (minutes): straight-line remaining distance ÷ recent average speed. Null when there is no
    /// current position, no job location, or too little movement history to estimate a speed.</summary>
    public static double? ComputeEtaMinutes(
        decimal? currentLat, decimal? currentLng,
        decimal? jobLat, decimal? jobLng,
        IReadOnlyList<ServiceRequestTripPositionEntity> recentNewestFirst)
    {
        if (currentLat is null || currentLng is null || jobLat is null || jobLng is null)
            return null;

        var speedKmh = RecentSpeedKmh(recentNewestFirst);
        if (speedKmh is not > 0)
            return null;

        var remainingKm = (double)GeoHelper.HaversineKm(currentLat.Value, currentLng.Value, jobLat.Value, jobLng.Value);
        return Math.Round(remainingKm / speedKmh.Value * 60.0, 1);
    }

    /// <summary>Recent average speed (km/h) over the sampled window, or null when there aren't ≥2 timed points.</summary>
    public static double? RecentSpeedKmh(IReadOnlyList<ServiceRequestTripPositionEntity> recentNewestFirst)
    {
        if (recentNewestFirst is null || recentNewestFirst.Count < 2)
            return null;

        // recentNewestFirst is newest→oldest; walk it as a window.
        var newest = recentNewestFirst[0];
        var oldest = recentNewestFirst[^1];
        var hours = (newest.PingAt - oldest.PingAt).TotalHours;
        if (hours <= 0) return null;

        double km = 0;
        for (var i = 0; i < recentNewestFirst.Count - 1; i++)
        {
            var a = recentNewestFirst[i];
            var b = recentNewestFirst[i + 1];
            km += (double)GeoHelper.HaversineKm(a.Latitude, a.Longitude, b.Latitude, b.Longitude);
        }
        var speed = km / hours;
        return speed > 0 ? speed : null;
    }

    /// <summary>Trail summary (total km, duration seconds) computed once on arrive/cancel before purging the raw rows.
    /// positions must be ordered oldest→newest. Returns (0,0) for a sparse trail rather than null so the columns fill.</summary>
    public static (decimal TotalKm, int DurationSeconds) ComputeSummary(IReadOnlyList<ServiceRequestTripPositionEntity> oldestFirst)
    {
        if (oldestFirst is null || oldestFirst.Count == 0)
            return (0m, 0);

        decimal totalKm = 0m;
        for (var i = 0; i < oldestFirst.Count - 1; i++)
        {
            var a = oldestFirst[i];
            var b = oldestFirst[i + 1];
            totalKm += GeoHelper.HaversineKm(a.Latitude, a.Longitude, b.Latitude, b.Longitude);
        }

        var seconds = (int)Math.Max(0, (oldestFirst[^1].PingAt - oldestFirst[0].PingAt).TotalSeconds);
        return (Math.Round(totalKm, 3), seconds);
    }
}
