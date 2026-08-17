using Aizen.Bff.Marine.Web.Application.Catalogue;
using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Web.Application.Contracts.ServicePages;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.ServicePages.Query.GetWebServicePage;

public sealed class GetWebServicePageQueryHandler
    : AizenQueryHandler<GetWebServicePageQuery, WebServicePageDto>
{
    private const string ServiceCategoryGroupCode = "SERVICE_PROVIDER_CATEGORY";
    private const string AvailabilityNone = "none";

    private readonly IReferenceDataRemoteCall _reference;
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<GetWebServicePageQueryHandler> _logger;

    public GetWebServicePageQueryHandler(
        IReferenceDataRemoteCall reference,
        IIdentityRemoteCall identity,
        ILogger<GetWebServicePageQueryHandler> logger)
    {
        _reference = reference;
        _identity = identity;
        _logger = logger;
    }

    public override async Task<WebServicePageDto?> Handle(
        GetWebServicePageQuery request, CancellationToken cancellationToken)
    {
        var slug = (request.ServiceSlug ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(slug))
            throw new AizenBusinessException("A service slug is required.");

        // City comes either directly (M2 interim) or by resolving a location slug to its city (M3). Availability is
        // city-keyed either way.
        var cityCode = await ResolveCityCode(request);
        if (string.IsNullOrWhiteSpace(cityCode))
            throw new AizenBusinessException("A cityCode or a resolvable locationSlug is required.");

        // Resolve serviceSlug → SERVICE_PROVIDER_CATEGORY item using the SAME deterministic slug transform the W4
        // catalogue derives — the reverse lookup. An unknown slug is a clean not-found, never faked.
        var item = await ResolveServiceBySlug(slug);
        if (item is null)
            throw new AizenBusinessException($"Unknown service '{slug}'.");

        var service = WebServiceMapper.ToSummary(item);

        // Coarse availability for (city, canonical Code). The module compares Code.ToLowerInvariant() against the
        // stored lower(Code). Fail-safe to "none" (never over-state availability) if the signal is unavailable.
        var availability = await ResolveAvailability(cityCode, item.Code);

        return new WebServicePageDto
        {
            Service = service,
            Location = new WebServicePageLocationDto { CityCode = cityCode },
            Availability = availability,
        };
    }

    private const string CityLocationType = "city";

    // Interim form: cityCode directly. M3 form: resolve locationSlug → location, then walk to the city (city itself,
    // or the "city" entry in its parent chain). Availability is city-keyed, so a district/neighborhood uses its city.
    private async Task<string> ResolveCityCode(GetWebServicePageQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.CityCode))
            return request.CityCode.Trim();

        var locationSlug = (request.LocationSlug ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(locationSlug))
            return string.Empty;

        try
        {
            var loc = (await _reference.GetLocationBySlug(locationSlug))?.Body
                ?? throw new AizenBusinessException($"Unknown location '{locationSlug}'.");

            if (string.Equals(loc.LocationType, CityLocationType, StringComparison.OrdinalIgnoreCase))
                return loc.Code;

            // district / neighborhood → the city ancestor drives the (city-keyed) availability. A country has none.
            return loc.ParentChain
                .FirstOrDefault(p => string.Equals(p.LocationType, CityLocationType, StringComparison.OrdinalIgnoreCase))
                ?.Code ?? string.Empty;
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Location slug '{Slug}' resolution failed (status {Status}).", locationSlug, ex.StatusCode);
            throw new AizenBusinessException("Location reference data is currently unavailable.");
        }
    }

    private async Task<LookupItemDto?> ResolveServiceBySlug(string slug)
    {
        try
        {
            var resp = await _reference.GetLookupItems(ServiceCategoryGroupCode);
            var items = resp?.Body ?? new();
            return items.FirstOrDefault(i =>
                string.Equals(WebServiceMapper.ToSlug(i.Code), slug, StringComparison.OrdinalIgnoreCase));
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Service catalogue lookup failed while resolving slug '{Slug}' (status {Status}).",
                slug, ex.StatusCode);
            throw new AizenBusinessException("The service catalogue is currently unavailable.");
        }
    }

    private async Task<string> ResolveAvailability(string cityCode, string categoryCode)
    {
        try
        {
            var resp = await _identity.GetProviderAreaAvailability(cityCode, categoryCode);
            var value = resp?.Body?.Availability;
            return string.IsNullOrWhiteSpace(value) ? AvailabilityNone : value;
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Availability signal unavailable for {City}/{Category} (status {Status}); defaulting to none.",
                cityCode, categoryCode, ex.StatusCode);
            return AvailabilityNone;
        }
    }
}
