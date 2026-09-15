using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.Cache.Abstraction;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

public sealed class GetProviderDiscoveryBffQueryHandler
    : AizenQueryHandler<GetProviderDiscoveryBffQuery, GetProviderDiscoveryBffResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly IVesselRemoteCall _vessel;
    private readonly ICargoDrySupplyRemoteCall _cargoDrySupply;
    private readonly IAizenDistributedCache _cache;
    private readonly ILogger<GetProviderDiscoveryBffQueryHandler> _logger;

    private const string CargoDrySupplyCategory = "CARGODRY_SUPPLY";
    private const string CargoDryNotProgramReason = "Only CargoDry program providers (active consignment agreement) can accept this request.";
    private const string CargoDryNoStockReason = "Stokta yok — bu ürün için uygun kit bulunmuyor.";
    private const string VesselCacheKeyPrefix = "vessel:summary:v3:";
    private static readonly TimeSpan VesselCacheTtl = TimeSpan.FromMinutes(10);

    public GetProviderDiscoveryBffQueryHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest,
        IVesselRemoteCall vessel,
        ICargoDrySupplyRemoteCall cargoDrySupply,
        IAizenDistributedCache cache,
        ILogger<GetProviderDiscoveryBffQueryHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _serviceRequest = serviceRequest;
        _vessel = vessel;
        _cargoDrySupply = cargoDrySupply;
        _cache = cache;
        _logger = logger;
    }

    public override async Task<GetProviderDiscoveryBffResponse?> Handle(
        GetProviderDiscoveryBffQuery request, CancellationToken ct)
    {
        // Resolve identity first (fail closed)
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
        {
            _logger.LogWarning("Provider profile not resolved. Returning empty discovery page.");
            return new GetProviderDiscoveryBffResponse { PageSize = request.PageSize };
        }

        // Map offerState enum name → int code (Any=0, NotOffered=1, Offered=2)
        int? offerStateCode = request.OfferState?.Trim().ToLowerInvariant() switch
        {
            "notoffered" => 1,
            "offered" => 2,
            _ => null,
        };

        // CargoDry supply flow (item 5): resolve (from CargoDry) the owner ids that prefer THIS provider and pass them
        // to discovery as a CSV, so the module sets IsOwnerPreferred per item WITHOUT exposing the raw OwnerUserId.
        // Page-independent (no product codes needed). Fail-safe: on a CargoDry read failure, no items are flagged.
        string? preferredOwnerCsv = null;
        try
        {
            var preferredCtx = await _cargoDrySupply.GetProviderContext(
                new Modules.CargoDry.Abstraction.RemoteCall.Requests.GetCargoDrySupplyProviderContextRemoteRequest
                {
                    ProviderProfileId = _identityHolder.ProfileId ?? 0,
                    ProductCodes = new List<string>(),
                });
            if (preferredCtx.PreferredOwnerUserIds.Count > 0)
                preferredOwnerCsv = string.Join(",", preferredCtx.PreferredOwnerUserIds.Take(500)); // cap keeps the URL bounded
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CargoDry preferred-owners lookup failed; IsPreferred left false on this page.");
        }

        // Call module discovery endpoint. A module business error (e.g. a bad filter) comes back as a non-2xx envelope,
        // on which Refit throws ApiException — surface it as a clean business error (400) instead of letting it bubble
        // up as an unhandled 500. (This is what turned the app's "centre without radius" call into a 500.)
        Modules.ServiceRequest.Abstraction.Response.Provider.ProviderDiscoveryResponse? page;
        try
        {
            var moduleResult = await _serviceRequest.GetProviderDiscovery(
                request.PageSize, request.Cursor, request.SortBy,
                request.LocationCityCode, request.LocationCountryCode,
                request.ServiceCategoryCode, request.MinPriority,
                request.SearchTerm, offerStateCode, request.PublishedAfterUtc,
                request.CenterLatitude, request.CenterLongitude, request.RadiusKm,
                request.BoundsMinLat, request.BoundsMaxLat,
                request.BoundsMinLng, request.BoundsMaxLng,
                preferredOwnerCsv);
            page = moduleResult.Body;
        }
        catch (Refit.ApiException ex)
        {
            var message = ExtractBusinessMessage(ex.Content) ?? "Failed to load discovery.";
            _logger.LogWarning(ex, "Discovery module call failed for provider {ProfileId}: {Message}",
                _identityHolder.ProfileId, message);
            throw new AizenBusinessException(message);
        }
        if (page is null || page.Items.Count == 0)
        {
            return new GetProviderDiscoveryBffResponse
            {
                NextCursor = page?.NextCursor,
                PageSize = page?.PageSize ?? request.PageSize,
                LocationMode = page?.LocationMode
            };
        }

        // Collect distinct VesselIds and resolve from cache / bulk call
        var vesselLookup = await ResolveVesselSummariesAsync(page.Items);

        // CargoDry supply flow: one batch call for the whole page — which requested products this provider may accept
        // (active consignment agreement + active product). Owners-preferring-me is also returned but cannot be applied
        // per-item because discovery deliberately omits OwnerUserId (privacy). See IsPreferred note below.
        var supplyProductCodes = page.Items
            .Where(i => !string.IsNullOrWhiteSpace(i.CargoDryProductCode))
            .Select(i => i.CargoDryProductCode!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // A1 — per requested product: active-agreement flag + available-kit count, so we can show a distinct
        // "not in program" vs "stokta yok" locked reason and surface the available count.
        var stockByCode = new Dictionary<string, Modules.CargoDry.Abstraction.RemoteCall.Responses.CargoDrySupplyProductStockRemoteDto>(StringComparer.OrdinalIgnoreCase);
        if (supplyProductCodes.Count > 0)
        {
            try
            {
                var ctx = await _cargoDrySupply.GetProviderContext(
                    new Modules.CargoDry.Abstraction.RemoteCall.Requests.GetCargoDrySupplyProviderContextRemoteRequest
                    {
                        ProviderProfileId = _identityHolder.ProfileId ?? 0,
                        ProductCodes = supplyProductCodes,
                    });
                foreach (var s in ctx.ProductStock) stockByCode[s.ProductCode] = s;
            }
            catch (Exception ex)
            {
                // Fail-safe: on a CargoDry read failure, leave supply items locked (CanAccept=false) rather than
                // letting the whole discovery page fail.
                _logger.LogWarning(ex, "CargoDry provider-context call failed; CARGODRY_SUPPLY items left locked on this page.");
            }
        }

        // Map module DTOs to BFF DTOs with vessel enrichment
        var items = page.Items.Select(item =>
        {
            vesselLookup.TryGetValue(item.VesselId, out var vessel);
            var isSupply = string.Equals(item.ServiceCategoryCode, CargoDrySupplyCategory, StringComparison.OrdinalIgnoreCase)
                           && !string.IsNullOrWhiteSpace(item.CargoDryProductCode);

            // A1 — 3-way gate for supply items: not-in-program / no-stock / ok. Non-supply items stay CanAccept=true.
            bool canAccept = true;
            string? canAcceptReason = null;
            int? availableKitCount = null;
            if (isSupply)
            {
                stockByCode.TryGetValue(item.CargoDryProductCode!, out var stock);
                availableKitCount = stock?.AvailableKitCount;
                if (stock is null || !stock.HasActiveAgreement)
                {
                    canAccept = false;
                    canAcceptReason = CargoDryNotProgramReason;
                }
                else if (stock.AvailableKitCount <= 0)
                {
                    canAccept = false;
                    canAcceptReason = CargoDryNoStockReason;
                }
            }
            return new ProviderDiscoveryBffItemDto
            {
                Id = item.Id,
                RequestCode = item.RequestCode,
                Title = item.Title,
                Description = item.Description,
                Status = item.Status,
                Priority = item.Priority,
                ServiceCategoryCode = item.ServiceCategoryCode,
                ServiceTypeCode = item.ServiceTypeCode,
                CargoDryProductCode = item.CargoDryProductCode,
                CanAccept = canAccept,
                CanAcceptReason = canAcceptReason,
                AvailableKitCount = availableKitCount,
                // Item 5: server-computed match (owner ∈ preferred set) — meaningful for CARGODRY_SUPPLY items.
                IsPreferred = isSupply && item.IsOwnerPreferred,
                LocationCityCode = item.LocationCityCode,
                LocationCountryCode = item.LocationCountryCode,
                LocationMarinaName = item.LocationMarinaName,
                SnappedLatitude = item.SnappedLatitude,
                SnappedLongitude = item.SnappedLongitude,
                DistanceKm = item.DistanceKm,
                RequestedStartDate = item.RequestedStartDate,
                RequestedEndDate = item.RequestedEndDate,
                ExpiresAt = item.ExpiresAt,
                PublishedAt = item.PublishedAt,
                OfferCount = item.OfferCount,
                AttachmentCount = item.AttachmentCount,
                HasProviderOffer = item.HasProviderOffer,
                ProviderOfferId = item.ProviderOfferId,
                ProviderOfferStatus = item.ProviderOfferStatus,
                ProviderOfferTotalAmount = item.ProviderOfferTotalAmount,
                IsUpdated = item.IsUpdated,
                VesselId = item.VesselId,
                VesselName = item.VesselName ?? vessel?.Name,
                VesselTypeCode = vessel?.VesselTypeCode,
                VesselBrand = vessel?.Brand,
                VesselModel = vessel?.Model,
                VesselLengthValue = vessel?.LengthValue,
                VesselLengthUnitCode = vessel?.LengthUnitCode,
            };
        }).ToList();

        return new GetProviderDiscoveryBffResponse
        {
            Items = items,
            NextCursor = page.NextCursor,
            PageSize = page.PageSize,
            LocationMode = page.LocationMode
        };
    }

    private static string? ExtractBusinessMessage(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("header", out var header)
                && header.TryGetProperty("errorMessage", out var msg)
                && msg.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var value = msg.GetString();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // Not an Aizen envelope
        }
        return null;
    }

    private async Task<Dictionary<long, VesselSummaryDto>> ResolveVesselSummariesAsync(
        List<Modules.ServiceRequest.Abstraction.Response.Provider.ProviderDiscoveryItemDto> items)
    {
        var distinctIds = items
            .Where(i => i.VesselId > 0)
            .Select(i => i.VesselId)
            .Distinct()
            .ToList();

        if (distinctIds.Count == 0)
            return new Dictionary<long, VesselSummaryDto>();

        var result = new Dictionary<long, VesselSummaryDto>();
        var uncachedIds = new List<long>();

        // Check cache for each vessel id
        foreach (var id in distinctIds)
        {
            try
            {
                var cached = await _cache.GetNoHash<VesselSummaryDto>($"{VesselCacheKeyPrefix}{id}");
                if (cached is not null)
                {
                    result[id] = cached;
                    continue;
                }
            }
            catch
            {
                // Cache miss or error — treat as uncached
            }

            uncachedIds.Add(id);
        }

        if (uncachedIds.Count == 0)
            return result;

        // One bulk call for all uncached ids
        try
        {
            var idsParam = string.Join(",", uncachedIds);
            var response = await _vessel.GetSummaries(idsParam);

            if (response.Body?.Items is { Count: > 0 })
            {
                foreach (var summary in response.Body.Items)
                {
                    result[summary.VesselId] = summary;

                    // Cache each returned summary
                    try
                    {
                        await _cache.SetNoHash($"{VesselCacheKeyPrefix}{summary.VesselId}", summary, VesselCacheTtl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to cache vessel summary for VesselId {VesselId}.", summary.VesselId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vessel summary bulk call failed for {Count} ids. Discovery page returned without vessel enrichment.", uncachedIds.Count);
        }

        return result;
    }
}
