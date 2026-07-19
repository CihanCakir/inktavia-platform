using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.Cache.Abstraction;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.MarineProvider.Application.Jobs;

public sealed class GetJobDetailBffQueryHandler
    : AizenQueryHandler<GetJobDetailBffQuery, GetProviderJobDetailResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _h;
    private readonly IServiceRequestRemoteCall _sr;
    private readonly IVesselRemoteCall _vessel;
    private readonly IAizenDistributedCache _cache;

    public GetJobDetailBffQueryHandler(
        IProviderProfileResolver resolver, IProviderIdentityHolder h,
        IServiceRequestRemoteCall sr, IVesselRemoteCall vessel, IAizenDistributedCache cache)
    {
        _resolver = resolver; _h = h; _sr = sr; _vessel = vessel; _cache = cache;
    }

    public override async Task<GetProviderJobDetailResponse?> Handle(
        GetJobDetailBffQuery q, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_h.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        GetProviderJobDetailResponse result;
        try
        {
            var response = await _sr.GetProviderJobDetail(q.AssignmentId);
            result = response.Body!;
        }
        catch (Refit.ApiException)
        {
            throw new AizenBusinessException("Job not found.");
        }

        // Vessel enrichment (non-fatal)
        if (result.Request is { VesselId: > 0 } req)
        {
            try
            {
                var cacheKey = $"vessel:summary:v3:{req.VesselId}";
                VesselSummaryDto? vessel = null;
                try { vessel = await _cache.GetNoHash<VesselSummaryDto>(cacheKey); } catch { }
                if (vessel is null)
                {
                    var vr = await _vessel.GetSummaries(req.VesselId.ToString());
                    vessel = vr.Body?.Items?.FirstOrDefault(v => v.VesselId == req.VesselId);
                    if (vessel is not null)
                        try { await _cache.SetNoHash(cacheKey, vessel, TimeSpan.FromMinutes(10)); } catch { }
                }
                if (vessel is not null)
                {
                    req.VesselName ??= vessel.Name;
                    req.VesselTypeCode = vessel.VesselTypeCode;
                    req.VesselBrand = vessel.Brand;
                    req.VesselModel = vessel.Model;
                    req.VesselLengthValue = vessel.LengthValue;
                    req.VesselLengthUnitCode = vessel.LengthUnitCode;
                    req.VesselYear = vessel.ProductionYear;
                    req.VesselMaterialCode = vessel.HullMaterialCode;
                    req.VesselRegistrationNumber = vessel.RegistrationNumber;
                    req.VesselBeamValue = vessel.BeamValue;
                    req.VesselBeamUnitCode = vessel.BeamUnitCode;
                    req.VesselDraftValue = vessel.DraftValue;
                    req.VesselDraftUnitCode = vessel.DraftUnitCode;
                }
            }
            catch { /* vessel enrichment non-fatal */ }
        }

        return result;
    }
}
