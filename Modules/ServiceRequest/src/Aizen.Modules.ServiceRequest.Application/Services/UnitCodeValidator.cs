using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Services;

/// <summary>
/// Validates offer-item UnitCode against ReferenceData measurement units.
/// Fetches the active unit set once per request (cached in Redis, long TTL — units change ~never).
/// </summary>
public sealed class UnitCodeValidator
{
    private const string CacheKey = "refdata:measurement-units:active";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    private readonly IServiceRequestReferenceDataRemoteCall _referenceData;
    private readonly IAizenDistributedCache _cache;
    private readonly ILogger<UnitCodeValidator> _logger;

    public UnitCodeValidator(
        IServiceRequestReferenceDataRemoteCall referenceData,
        IAizenDistributedCache cache,
        ILogger<UnitCodeValidator> logger)
    {
        _referenceData = referenceData;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Validates all non-empty UnitCodes in the given items. Throws AizenBusinessException on first unknown code.
    /// Null/empty UnitCode is accepted — only a non-empty unrecognised code is rejected.
    /// </summary>
    public async Task ValidateUnitCodesAsync(IEnumerable<Abstraction.Request.Offer.CreateServiceRequestOfferItemRequest> items, CancellationToken ct)
    {
        var codesToValidate = items
            .Where(i => !string.IsNullOrWhiteSpace(i.UnitCode))
            .Select(i => i.UnitCode!.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        if (codesToValidate.Count == 0) return;

        var validCodes = await GetActiveUnitCodesAsync(ct);
        if (validCodes is null) return; // Service unavailable — skip validation

        foreach (var code in codesToValidate)
        {
            if (!validCodes.Contains(code))
                throw new AizenBusinessException($"SR_OFFER_UNKNOWN_UNIT: '{code}'");
        }
    }

    private async Task<HashSet<string>?> GetActiveUnitCodesAsync(CancellationToken ct)
    {
        // Try cache first
        try
        {
            var cached = await _cache.GetNoHash<List<string>>(CacheKey);
            if (cached is { Count: > 0 })
                return new HashSet<string>(cached, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            // Cache miss or error
        }

        // Fetch from ReferenceData
        try
        {
            var response = await _referenceData.GetActiveMeasurementUnits();
            var codes = response.Body?
                .Where(u => u.IsActive)
                .Select(u => u.Code.ToUpperInvariant())
                .Distinct()
                .ToList() ?? new List<string>();

            if (codes.Count > 0)
            {
                try { await _cache.SetNoHash(CacheKey, codes, CacheTtl); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to cache unit codes."); }
            }

            return new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch measurement units from ReferenceData. Unit validation skipped.");
            return null; // Service unavailable — skip validation
        }
    }
}
