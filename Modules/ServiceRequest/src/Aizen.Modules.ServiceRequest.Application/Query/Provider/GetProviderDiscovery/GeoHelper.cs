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
