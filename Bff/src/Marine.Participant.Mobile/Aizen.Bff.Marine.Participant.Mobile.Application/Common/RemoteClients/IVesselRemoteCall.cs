using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Request.Document;
using Aizen.Modules.Vessel.Abstraction.Request.Engine;
using Aizen.Modules.Vessel.Abstraction.Request.Location;
using Aizen.Modules.Vessel.Abstraction.Request.Media;
using Aizen.Modules.Vessel.Abstraction.Request.Specification;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Document;
using Aizen.Modules.Vessel.Abstraction.Response.Engine;
using Aizen.Modules.Vessel.Abstraction.Response.Location;
using Aizen.Modules.Vessel.Abstraction.Response.Media;
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

    // Resolve a vessel by its globally-unique VesselCode. Used post-create to recover the committed numeric
    // Id deterministically (the create response DTO is built pre-commit so its Id is 0), without re-scoping
    // the caller's list — a brand-new code is never cached, so this read hits the DB and returns the real Id.
    [AizenRemoteCallGet("/api/v1/vessels/code/{vesselCode}")]
    Task<AizenApiResponse<GetVesselByCodeResponse>> GetVesselByCode(string vesselCode);

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

    // ── M4c edit (owner-gated by the module's EnsureCanEditAsync via the asserted caller) ──────────
    // Update the core profile (full controlled update — the BFF merges partial input over current values).
    [AizenRemoteCallPut("/api/v1/vessels/{vesselId}")]
    Task<AizenApiResponse<UpdateVesselResponse>> UpdateVessel(
        long vesselId, [AizenRemoteCallBody] UpdateVesselRequest request);

    // ── Location (owner-gated by the module's EnsureCanEditAsync via the asserted caller) ──────────
    // Record the auto-detected CURRENT position as a new location snapshot (append-only; module marks the prior
    // snapshot historical). The mobile PUT passes lat/lng with Source="device"; the snapshot backs detail's
    // CurrentLocation and the list card's last-location fields.
    [AizenRemoteCallPut("/api/v1/vessels/{vesselId}/location")]
    Task<AizenApiResponse<UpdateVesselLocationSnapshotResponse>> UpdateVesselLocation(
        long vesselId, [AizenRemoteCallBody] UpdateVesselLocationSnapshotRequest request);

    // Set the owner's EXPLICIT location choice on the vessel aggregate. For a marina pick the BFF resolves the
    // marina name/coords from ReferenceData first and passes them denormalized, so the Vessel module never has to
    // call ReferenceData. An all-null body clears the selection.
    [AizenRemoteCallPut("/api/v1/vessels/{vesselId}/location/selected")]
    Task<AizenApiResponse<SetVesselSelectedLocationResponse>> SetVesselSelectedLocation(
        long vesselId, [AizenRemoteCallBody] SetVesselSelectedLocationRequest request);

    // Update an existing engine row (the wizard edits the single/primary engine).
    [AizenRemoteCallPut("/api/v1/vessels/{vesselId}/engines/{engineId}")]
    Task<AizenApiResponse<UpdateVesselEngineResponse>> UpdateEngine(
        long vesselId, long engineId, [AizenRemoteCallBody] UpdateVesselEngineRequest request);

    // ── M4d archive / restore / status (all owner-gated by the module's EnsureCanEditAsync) ──────────
    // Archive the vessel (soft — no hard delete). Sets IsArchived; the module invalidates the vessel + the
    // default user list page (0,20), so it drops from the mobile active list immediately.
    [AizenRemoteCallPatch("/api/v1/vessels/{vesselId}/archive")]
    Task<AizenApiResponse<ArchiveVesselResponse>> ArchiveVessel(
        long vesselId, [AizenRemoteCallBody] ArchiveVesselRequest request);

    // Restore an archived vessel back to the active list (module clears IsArchived + re-invalidates the list page).
    [AizenRemoteCallPatch("/api/v1/vessels/{vesselId}/restore")]
    Task<AizenApiResponse<RestoreVesselResponse>> RestoreVessel(long vesselId);

    // Change the operational status (Active/Passive/UnderMaintenance) — module enforces the valid-transition graph.
    [AizenRemoteCallPatch("/api/v1/vessels/{vesselId}/status")]
    Task<AizenApiResponse<UpdateVesselStatusResponse>> UpdateStatus(
        long vesselId, [AizenRemoteCallBody] UpdateVesselStatusRequest request);

    // ── M4e documents (owner-gated by the module's EnsureCanEditAsync on add/remove) ────────────────
    // List the vessel's documents. The mobile BFF reads the DEFAULT page with includeAccessUrls=false — the
    // exact key the module's InvalidateDocumentsAsync evicts on add/remove — so the list is fresh with no flush;
    // the BFF resolves each doc's presigned read URL itself (per-doc), avoiding the never-invalidated
    // includeAccessUrls=true cache variant. FileId is present regardless of includeAccessUrls.
    [AizenRemoteCallGet("/api/v1/vessels/{vesselId}/documents")]
    Task<AizenApiResponse<GetVesselDocumentsResponse>> GetVesselDocuments(
        long vesselId,
        [Refit.Query] int pageIndex = 0, [Refit.Query] int pageSize = 20,
        [Refit.Query] bool includeAccessUrls = false, [Refit.Query] int accessUrlExpiresInMinutes = 15);

    // Attach an already-uploaded file (FileId) to the vessel as a typed document. The module invalidates the
    // documents read (default page) so the mobile list refreshes immediately.
    [AizenRemoteCallPost("/api/v1/vessels/{vesselId}/documents")]
    Task<AizenApiResponse<AddVesselDocumentResponse>> AddVesselDocument(
        long vesselId, [AizenRemoteCallBody] AddVesselDocumentRequest request);

    // Remove a document (module invalidates the documents read).
    [AizenRemoteCallDelete("/api/v1/vessels/{vesselId}/documents/{documentId}")]
    Task<AizenApiResponse<RemoveVesselDocumentResponse>> RemoveVesselDocument(long vesselId, long documentId);

    // ── M4f media / photos (owner-gated by the module's EnsureCanEditAsync on add/remove/set-cover) ──────
    // List the vessel's media. As with documents, the BFF reads the DEFAULT includeAccessUrls=false page (the key
    // InvalidateMediaAsync evicts) and resolves each item's presigned read URL itself.
    [AizenRemoteCallGet("/api/v1/vessels/{vesselId}/media")]
    Task<AizenApiResponse<GetVesselMediaResponse>> GetVesselMedia(
        long vesselId,
        [Refit.Query] int pageIndex = 0, [Refit.Query] int pageSize = 20,
        [Refit.Query] bool includeAccessUrls = false, [Refit.Query] int accessUrlExpiresInMinutes = 15);

    // Attach an already-uploaded file (client-side presigned, then completed) as a vessel photo.
    [AizenRemoteCallPost("/api/v1/vessels/{vesselId}/media")]
    Task<AizenApiResponse<AddVesselMediaResponse>> AddVesselMedia(
        long vesselId, [AizenRemoteCallBody] AddVesselMediaRequest request);

    // Remove a media item (module deactivates + invalidates media/list/detail).
    [AizenRemoteCallDelete("/api/v1/vessels/{vesselId}/media/{mediaId}")]
    Task<AizenApiResponse<RemoveVesselMediaResponse>> RemoveVesselMedia(long vesselId, long mediaId);

    // Set a media item as the cover (module clears other covers + invalidates media/list/detail).
    [AizenRemoteCallPatch("/api/v1/vessels/{vesselId}/media/{mediaId}/set-cover")]
    Task<AizenApiResponse<SetCoverVesselMediaResponse>> SetCoverVesselMedia(long vesselId, long mediaId);
}
