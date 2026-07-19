using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Common.Warnings;
using Aizen.Bff.MarineProvider.Application.Contracts.Jobs;
using Aizen.Core.Cache.Abstraction;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Jobs;

/// <summary>
/// Resolves the provider profile first (this populates the identity holder so the outgoing auth handler attaches
/// the trusted-BFF identity assertion), then calls the ServiceRequest module which scopes the result by the
/// asserted provider profile id. Downstream failures degrade to a warning rather than an error.
/// </summary>
public sealed class GetProviderJobsQueryHandler
    : AizenQueryHandler<GetProviderJobsQuery, GetProviderJobsResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly IVesselRemoteCall _vessel;
    private readonly IAizenDistributedCache _cache;
    private readonly ILogger<GetProviderJobsQueryHandler> _logger;

    private const string VesselCacheKeyPrefix = "vessel:summary:v3:";
    private static readonly TimeSpan VesselCacheTtl = TimeSpan.FromMinutes(10);

    public GetProviderJobsQueryHandler(
        IProviderProfileResolver resolver,
        IServiceRequestRemoteCall serviceRequest,
        IVesselRemoteCall vessel,
        IAizenDistributedCache cache,
        ILogger<GetProviderJobsQueryHandler> logger)
    {
        _resolver = resolver;
        _serviceRequest = serviceRequest;
        _vessel = vessel;
        _cache = cache;
        _logger = logger;
    }

    public override async Task<GetProviderJobsResponse?> Handle(
        GetProviderJobsQuery request, CancellationToken cancellationToken)
    {
        var response = new GetProviderJobsResponse
        {
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };

        // Resolve identity first → populates IProviderIdentityHolder → assertion headers on the module call.
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        response.HasProfileLink = resolution.ProfileId is { } id && id > 0;

        if (!response.HasProfileLink)
        {
            response.Message = "Your account is not linked to a provider profile yet.";
            return response;
        }

        try
        {
            var result = await _serviceRequest.GetProviderJobs(request.PageIndex, request.PageSize);
            var body = result?.Body;

            if (body is not null)
            {
                // Bulk-enrich vessel names
                var vesselLookup = await ResolveVesselNamesAsync(body.Items, cancellationToken);

                response.Items = body.Items
                    .Select(j =>
                    {
                        vesselLookup.TryGetValue(j.VesselId, out var vesselName);
                        return new ProviderJobDto
                        {
                            AssignmentId = j.AssignmentId,
                            ServiceRequestId = j.ServiceRequestId,
                            ServiceRequestOfferId = j.ServiceRequestOfferId,
                            Status = j.Status,
                            Title = j.Title,
                            RequestCode = j.RequestCode,
                            VesselId = j.VesselId,
                            VesselName = j.VesselName ?? vesselName,
                            ScheduledStartDate = j.ScheduledStartDate,
                            ScheduledEndDate = j.ScheduledEndDate,
                            ActualStartDate = j.ActualStartDate,
                            ActualEndDate = j.ActualEndDate,
                            ProviderNotes = j.ProviderNotes
                        };
                    })
                    .ToList();
            }

            response.TotalReturned = response.Items.Count;
            response.Message = "OK";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ServiceRequest provider jobs call failed.");
            response.Message = "Jobs are temporarily unavailable.";
            response.Warnings.Add(ProviderBffWarning.CallFailed("ServiceRequest.GetProviderJobs", ex.GetType().Name));
        }

        return response;
    }

    private async Task<Dictionary<long, string>> ResolveVesselNamesAsync(
        List<Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs.ProviderJobItemDto> items, CancellationToken ct)
    {
        var result = new Dictionary<long, string>();
        var distinctIds = items.Where(i => i.VesselId > 0).Select(i => i.VesselId).Distinct().ToList();
        if (distinctIds.Count == 0) return result;

        var uncachedIds = new List<long>();
        foreach (var id in distinctIds)
        {
            try
            {
                var cached = await _cache.GetNoHash<VesselSummaryDto>($"{VesselCacheKeyPrefix}{id}");
                if (cached is not null) { result[id] = cached.Name; continue; }
            }
            catch { }
            uncachedIds.Add(id);
        }

        if (uncachedIds.Count == 0) return result;

        try
        {
            var response = await _vessel.GetSummaries(string.Join(",", uncachedIds));
            if (response.Body?.Items is { Count: > 0 })
            {
                foreach (var s in response.Body.Items)
                {
                    result[s.VesselId] = s.Name;
                    try { await _cache.SetNoHash($"{VesselCacheKeyPrefix}{s.VesselId}", s, VesselCacheTtl); }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vessel summary bulk call failed for jobs. Returning without vessel names.");
        }

        return result;
    }
}
