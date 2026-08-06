using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Request.Engine;
using Aizen.Modules.Vessel.Abstraction.Request.Specification;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Engine;
using Aizen.Modules.Vessel.Abstraction.Response.Specification;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;

/// <summary>
/// BFF → Vessel module calls. `current-user` scopes to the caller via UserInfo.UserId, which the BFF supplies
/// through the identity assertion (resolve-by-subject sets the holder). Detail is by id with no module-side
/// ownership check — the BFF gates it against the caller's owned-vessel set. The write trio (create → spec →
/// engine, M4b) all resolve the caller as owner through that same assertion: create assigns the caller as
/// PrimaryOwner, and the spec/engine writes pass the module's `EnsureCanEditAsync` ownership check.
/// </summary>
public interface IVesselRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/vessels/current-user")]
    Task<AizenApiResponse<GetUserVesselsResponse>> GetUserVessels(
        [Refit.Query] int pageIndex = 0, [Refit.Query] int pageSize = 100);

    [AizenRemoteCallGet("/api/v1/vessels/{vesselId}")]
    Task<AizenApiResponse<GetVesselDetailResponse>> GetVesselDetail(long vesselId);

    // ── M4b create trio (owner = asserted caller) ──────────────────────────────────
    // Create the vessel core; the module assigns the asserted caller as PrimaryOwner (ignores OwnerUserId).
    [AizenRemoteCallPost("/api/v1/vessels")]
    Task<AizenApiResponse<CreateVesselResponse>> CreateVessel([AizenRemoteCallBody] CreateVesselRequest request);

    // Upsert the physical/technical spec (units stored as free-form codes; no reference FK).
    [AizenRemoteCallPut("/api/v1/vessels/{vesselId}/specification")]
    Task<AizenApiResponse<UpsertVesselSpecificationResponse>> UpsertSpecification(
        long vesselId, [AizenRemoteCallBody] UpsertVesselSpecificationRequest request);

    // Add one engine row (the wizard collects a single engine configuration).
    [AizenRemoteCallPost("/api/v1/vessels/{vesselId}/engines")]
    Task<AizenApiResponse<AddVesselEngineResponse>> AddEngine(
        long vesselId, [AizenRemoteCallBody] AddVesselEngineRequest request);
}
