using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.Cache.Abstraction;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

/// <summary>
/// One service request, for the provider. Enriches with vessel data from a single bulk call.
///
/// The access check lives in the module — this handler resolves the provider identity so the module knows
/// who is asking, then enriches with vessel data.
/// </summary>
public sealed class GetServiceRequestDetailBffQueryHandler
    : AizenQueryHandler<GetServiceRequestDetailBffQuery, GetProviderServiceRequestDetailResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly IVesselRemoteCall _vessel;
    private readonly IAizenDistributedCache _cache;
    private readonly ILogger<GetServiceRequestDetailBffQueryHandler> _logger;

    private const string VesselCacheKeyPrefix = "vessel:summary:v3:";
    private static readonly TimeSpan VesselCacheTtl = TimeSpan.FromMinutes(10);

    public GetServiceRequestDetailBffQueryHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest,
        IVesselRemoteCall vessel,
        IAizenDistributedCache cache,
        ILogger<GetServiceRequestDetailBffQueryHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _serviceRequest = serviceRequest;
        _vessel = vessel;
        _cache = cache;
        _logger = logger;
    }

    public override async Task<GetProviderServiceRequestDetailResponse?> Handle(
        GetServiceRequestDetailBffQuery request, CancellationToken ct)
    {
        var resolution = await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        // LOCKED DECISION: distance uses the provider's FIXED business location (profile), never the live/browser
        // position. Ignore any client-passed center and feed the module the profile's business coordinates, so the
        // distance the provider sees here equals the snapshot taken at offer create.
        var businessLat = resolution.Profile?.BusinessLatitude;
        var businessLng = resolution.Profile?.BusinessLongitude;
        var ratePerKm = resolution.Profile?.RatePerKm;

        GetProviderServiceRequestDetailResponse result;
        try
        {
            var response = await _serviceRequest.GetServiceRequestDetail(
                request.ServiceRequestId, businessLat, businessLng);
            result = response.Body ?? throw new AizenBusinessException("Service request not found.");
        }
        catch (Refit.ApiException ex)
        {
            var message = ExtractBusinessMessage(ex.Content) ?? "Service request not found.";
            _logger.LogWarning(ex, "Service request {ServiceRequestId} not readable by provider {ProfileId}: {Message}",
                request.ServiceRequestId, _identityHolder.ProfileId, message);
            throw new AizenBusinessException(message);
        }

        // Phase-1 quote helpers: echo the provider's rate and pre-compute the suggested travel fee
        // (distanceKm × ratePerKm) so the portal can pre-fill an editable "Yol bedeli / Travel fee" line.
        if (result.Detail?.Request is not null)
        {
            result.Detail.Request.RatePerKm = ratePerKm;
            var dist = result.Detail.Request.DistanceKm;
            if (dist.HasValue && ratePerKm is > 0m)
                result.Detail.Request.SuggestedTravelFee = Math.Round(dist.Value * ratePerKm.Value, 2);
        }

        // Enrich with vessel data — one call
        await EnrichVesselAsync(result);

        return result;
    }

    private async Task EnrichVesselAsync(GetProviderServiceRequestDetailResponse response)
    {
        var vesselId = response.Detail?.Request?.VesselId ?? 0;
        if (vesselId <= 0) return;

        VesselSummaryDto? vessel = null;

        // Check cache
        try
        {
            vessel = await _cache.GetNoHash<VesselSummaryDto>($"{VesselCacheKeyPrefix}{vesselId}");
        }
        catch
        {
            // Cache miss or error
        }

        if (vessel is not null)
        {
            ApplyVessel(response, vessel);
            return;
        }

        // One bulk call with a single id
        try
        {
            var vesselResponse = await _vessel.GetSummaries(vesselId.ToString());
            vessel = vesselResponse.Body?.Items?.FirstOrDefault(v => v.VesselId == vesselId);

            if (vessel is not null)
            {
                ApplyVessel(response, vessel);
                try
                {
                    await _cache.SetNoHash($"{VesselCacheKeyPrefix}{vesselId}", vessel, VesselCacheTtl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cache vessel summary for VesselId {VesselId}.", vesselId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vessel summary call failed for VesselId {VesselId}. Detail returned without vessel enrichment.", vesselId);
        }
    }

    private static void ApplyVessel(GetProviderServiceRequestDetailResponse response, VesselSummaryDto vessel)
    {
        if (response.Detail?.Request is null) return;
        response.Detail.Request.VesselName ??= vessel.Name;
        response.Detail.Request.VesselTypeCode = vessel.VesselTypeCode;
        response.Detail.Request.VesselBrand = vessel.Brand;
        response.Detail.Request.VesselModel = vessel.Model;
        response.Detail.Request.VesselLengthValue = vessel.LengthValue;
        response.Detail.Request.VesselLengthUnitCode = vessel.LengthUnitCode;
        response.Detail.Request.VesselYear = vessel.ProductionYear;
        response.Detail.Request.VesselMaterialCode = vessel.HullMaterialCode;
        response.Detail.Request.VesselRegistrationNumber = vessel.RegistrationNumber;
        response.Detail.Request.VesselBeamValue = vessel.BeamValue;
        response.Detail.Request.VesselBeamUnitCode = vessel.BeamUnitCode;
        response.Detail.Request.VesselDraftValue = vessel.DraftValue;
        response.Detail.Request.VesselDraftUnitCode = vessel.DraftUnitCode;
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
}
