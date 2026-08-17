using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Application.Services.Travel;

/// <summary>
/// BE-S4b/S4c — at acceptance, gathers each Travel offer line's <c>TravelPricingDetail</c>, resolves the (denormalized) origin
/// and destination city labels via the GetCity remote call, and projects them to the acceptance-economics request DTOs keyed by
/// LineRef (= offer item id). Payment writes them as immutable <c>TravelPricingSnapshot</c> children of the aggregate economics
/// snapshot. The detail was already validated at offer-build time (S4a); this path does <b>no re-validation</b> and no distance
/// compute (the km is provider-declared; geo compute is deferred to GeoDiscovery).
/// </summary>
public sealed class TravelPricingSnapshotResolver
{
    private const string DefaultCountryCode = "TR";

    private readonly ServiceRequestDbContext _db;
    private readonly IServiceRequestReferenceDataRemoteCall _referenceData;

    public TravelPricingSnapshotResolver(ServiceRequestDbContext db, IServiceRequestReferenceDataRemoteCall referenceData)
    { _db = db; _referenceData = referenceData; }

    public async Task<IReadOnlyDictionary<string, CalculateServiceRequestEconomicsTravelDto>> ResolveForOfferAsync(
        ServiceRequestOfferEntity offer, CancellationToken ct)
    {
        var empty = new Dictionary<string, CalculateServiceRequestEconomicsTravelDto>();

        var itemIds = offer.Items.Select(i => i.Id).ToList();
        if (itemIds.Count == 0) return empty;

        var details = await _db.TravelPricingDetails.AsNoTracking()
            .Where(t => itemIds.Contains(t.OfferItemId))
            .ToListAsync(ct);
        if (details.Count == 0) return empty;

        // Resolve each distinct city code's label once (denormalized into the snapshot; no re-lookup after acceptance).
        var labels = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in details.SelectMany(d => new[] { d.OriginCityCode, d.DestinationCityCode })
                     .Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (labels.ContainsKey(code)) continue;
            var city = await _referenceData.GetCity(DefaultCountryCode, code);
            labels[code] = city.Body?.Name;
        }

        var result = new Dictionary<string, CalculateServiceRequestEconomicsTravelDto>();
        foreach (var d in details)
        {
            result[d.OfferItemId.ToString()] = new CalculateServiceRequestEconomicsTravelDto
            {
                Method               = (int)d.Method,
                OriginCityCode       = d.OriginCityCode,
                OriginCityLabel      = Label(labels, d.OriginCityCode),
                DestinationCityCode  = d.DestinationCityCode,
                DestinationCityLabel = Label(labels, d.DestinationCityCode),
                DistanceKm           = d.DistanceKm,
                PerKmRate            = d.PerKmRate,
                UnitCode             = d.UnitCode,
            };
        }
        return result;
    }

    private static string? Label(IReadOnlyDictionary<string, string?> labels, string? code)
        => string.IsNullOrWhiteSpace(code) ? null : (labels.TryGetValue(code.Trim(), out var name) ? name : null);
}
