using System.Security.Cryptography;
using System.Text;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.Cache.Abstraction;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

public sealed class GetDiscoverySummaryBffQueryHandler
    : AizenQueryHandler<GetDiscoverySummaryBffQuery, ProviderDiscoverySummaryResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly IAizenDistributedCache _cache;
    private readonly ILogger<GetDiscoverySummaryBffQueryHandler> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public GetDiscoverySummaryBffQueryHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest,
        IAizenDistributedCache cache,
        ILogger<GetDiscoverySummaryBffQueryHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _serviceRequest = serviceRequest;
        _cache = cache;
        _logger = logger;
    }

    public override async Task<ProviderDiscoverySummaryResponse?> Handle(
        GetDiscoverySummaryBffQuery request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
        {
            _logger.LogWarning("Provider profile not resolved. Returning empty discovery summary.");
            return new ProviderDiscoverySummaryResponse();
        }

        var filterHash = BuildFilterHash(request);
        var cacheKey = $"discovery:summary:{_identityHolder.ProfileId}:{filterHash}";

        // Check cache first
        try
        {
            var cached = await _cache.GetNoHash<ProviderDiscoverySummaryResponse>(cacheKey);
            if (cached is not null)
                return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache read failed for discovery summary.");
        }

        var result = await _serviceRequest.GetDiscoverySummary(
            request.LocationCityCode, request.LocationCountryCode,
            request.ServiceCategoryCode, request.SearchTerm,
            request.CenterLatitude, request.CenterLongitude, request.RadiusKm,
            request.BoundsMinLat, request.BoundsMaxLat,
            request.BoundsMinLng, request.BoundsMaxLng);

        var body = result.Body;

        // Cache the result
        if (body is not null)
        {
            try
            {
                await _cache.SetNoHash(cacheKey, body, CacheTtl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache write failed for discovery summary.");
            }
        }

        return body;
    }

    private static string BuildFilterHash(GetDiscoverySummaryBffQuery q)
    {
        var sb = new StringBuilder();
        sb.Append(q.LocationCityCode).Append('|');
        sb.Append(q.LocationCountryCode).Append('|');
        sb.Append(q.ServiceCategoryCode).Append('|');
        sb.Append(q.SearchTerm).Append('|');
        sb.Append(q.CenterLatitude).Append('|');
        sb.Append(q.CenterLongitude).Append('|');
        sb.Append(q.RadiusKm).Append('|');
        sb.Append(q.BoundsMinLat).Append('|');
        sb.Append(q.BoundsMaxLat).Append('|');
        sb.Append(q.BoundsMinLng).Append('|');
        sb.Append(q.BoundsMaxLng);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes)[..16];
    }
}
