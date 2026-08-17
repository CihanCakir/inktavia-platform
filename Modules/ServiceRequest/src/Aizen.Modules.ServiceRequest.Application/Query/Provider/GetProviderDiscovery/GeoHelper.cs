// ⚠️ GEO ON LOAN — ARCHITECTURAL EXCEPTION.
// Live geo search belongs to the future GeoDiscovery module (project rule). Kept here to ship provider
// discovery. Keep it isolated: no geo logic in domain entities, in the BFF, or in the SPA.
// Migration triggers: persistent provider coordinates · provider service-area onboarding · polygon service
// areas · geo ranking · more than ~10^5 candidate rows per query.

namespace Aizen.Modules.ServiceRequest.Application.Query.Provider.GetProviderDiscovery;

/// <summary>
/// Server-side bounding box computation from (center, radiusKm).
/// The bbox is the indexed prefilter; exact distance is computed in SQL on the small candidate set.
/// </summary>
public static class GeoHelper
{
    private const double EarthRadiusKm = 6371.0;

    /// <summary>
    /// Haversine distance between two points in km. Same formula as the SQL expression in discovery.
    /// </summary>
    public static decimal HaversineKm(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
    {
        var dLat = ((double)lat2 - (double)lat1) * Math.PI / 360.0;
        var dLng = ((double)lng2 - (double)lng1) * Math.PI / 360.0;
        var a = Math.Pow(Math.Sin(dLat), 2) +
                Math.Cos((double)lat1 * Math.PI / 180.0) * Math.Cos((double)lat2 * Math.PI / 180.0) *
                Math.Pow(Math.Sin(dLng), 2);
        return (decimal)(EarthRadiusKm * 2.0 * Math.Asin(Math.Sqrt(a)));
    }

    public static (decimal minLat, decimal maxLat, decimal minLng, decimal maxLng) BoundingBox(
        decimal centerLat, decimal centerLng, decimal radiusKm)
    {
        var latRad = (double)centerLat * Math.PI / 180.0;
        var deltaLat = (double)radiusKm / EarthRadiusKm * (180.0 / Math.PI);
        var deltaLng = deltaLat / Math.Cos(latRad);

        return (
            (decimal)((double)centerLat - deltaLat),
            (decimal)((double)centerLat + deltaLat),
            (decimal)((double)centerLng - deltaLng),
            (decimal)((double)centerLng + deltaLng)
        );
    }
}
