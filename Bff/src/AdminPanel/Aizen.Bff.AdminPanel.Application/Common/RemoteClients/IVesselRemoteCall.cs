using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Refit;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Document;
using Aizen.Modules.Vessel.Abstraction.Response.Media;
using Aizen.Modules.Vessel.Abstraction.Response.Ownership;
using Aizen.Modules.Vessel.Abstraction.Response.Status;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;
using Aizen.Modules.Vessel.Abstraction.Dto.Status;
using Aizen.Modules.Vessel.Abstraction.Dto.CatalogReference;
using Aizen.Modules.Vessel.Abstraction.Request.CatalogReference;
using Aizen.Bff.AdminPanel.Application.Dashboard.Dto;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Vessel admin BFF remote call",
    "Defines synchronous BFF-to-Vessel calls for admin vessel management and inspection. " +
    "Auth headers are injected automatically by AdminPanelBffAuthDelegatingHandler.")]
public interface IVesselRemoteCall : IAizenRemoteCall
{
    // ── Catalog references (owned by the Vessel module): review counts + merge repoint ──
    [AizenRemoteCallGet("/api/v1/admin/vessels/catalog-references/counts")]
    Task<AizenApiResponse<CatalogReferenceCountsDto>> GetCatalogReferenceCounts();
    [AizenRemoteCallPost("/api/v1/admin/vessels/catalog-references/repoint")]
    Task<AizenApiResponse<CatalogRepointResultDto>> RepointCatalogReference([AizenRemoteCallBody] RepointCatalogReferenceRequest request);

    [AizenRemoteCallGet("/api/v1/admin/vessels")]
    Task<AizenApiResponse<GetAllVesselsAdminResponse>> GetAdminVesselList(
        [Refit.Query] int      pageIndex           = 0,
        [Refit.Query] int      pageSize            = 20,
        [Refit.Query] string?  searchTerm          = null,
        [Refit.Query] bool?    isArchived          = null,
        [Refit.Query] int[]?   assetTypes          = null,
        [Refit.Query] int[]?   ownershipStatuses   = null,
        [Refit.Query] int[]?   operationalStatuses = null,
        [Refit.Query] long?    ownerUserId         = null);

    // C3 — dashboard fleet-status chart: per-status vessel counts (status = lowercase enum key).
    [AizenRemoteCallGet("/api/v1/admin/vessels/stats/status-counts")]
    Task<AizenApiResponse<List<StatusCountDto>>> GetVesselStatusCounts();

    [AizenRemoteCallGet("/api/v1/vessels/{vesselId}")]
    Task<AizenApiResponse<GetVesselDetailResponse>> GetVesselById(long vesselId);

    [AizenRemoteCallPut("/api/v1/vessels/{vesselId}")]
    Task<AizenApiResponse<UpdateVesselResponse>> UpdateVessel(
        long vesselId,
        [AizenRemoteCallBody] UpdateVesselRequest request);

    [AizenRemoteCallGet("/api/v1/vessels/{vesselId}/owners")]
    Task<AizenApiResponse<GetVesselOwnersResponse>> GetVesselOwners(
        long vesselId,
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize  = 20);

    [AizenRemoteCallGet("/api/v1/vessels/{vesselId}/documents")]
    Task<AizenApiResponse<GetVesselDocumentsResponse>> GetVesselDocuments(
        long vesselId,
        [Refit.Query] int  pageIndex                  = 0,
        [Refit.Query] int  pageSize                   = 50,
        [Refit.Query] bool includeAccessUrls          = false,
        [Refit.Query] int  accessUrlExpiresInMinutes  = 60);

    [AizenRemoteCallDelete("/api/v1/vessels/{vesselId}/documents/{documentId}")]
    Task<AizenApiResponse<RemoveVesselDocumentResponse>> RemoveVesselDocument(
        long vesselId,
        long documentId);

    [AizenRemoteCallPatch("/api/v1/vessels/{vesselId}/documents/{documentId}/approve")]
    Task<AizenApiResponse<ApproveVesselDocumentResponse>> ApproveVesselDocument(
        long vesselId,
        long documentId);

    [AizenRemoteCallPost("/api/v1/vessels/{vesselId}/documents")]
    Task<AizenApiResponse<AddVesselDocumentResponse>> AddVesselDocument(
        long vesselId,
        [AizenRemoteCallBody] Aizen.Modules.Vessel.Abstraction.Request.Document.AddVesselDocumentRequest request);

    [AizenRemoteCallPut("/api/v1/vessels/{vesselId}/documents/{documentId}")]
    Task<AizenApiResponse<UpdateVesselDocumentResponse>> UpdateVesselDocument(
        long vesselId,
        long documentId,
        [AizenRemoteCallBody] Aizen.Modules.Vessel.Abstraction.Request.Document.UpdateVesselDocumentRequest request);

    [AizenRemoteCallPost("/api/v1/vessels/{vesselId}/media")]
    Task<AizenApiResponse<AddVesselMediaResponse>> AddVesselMedia(
        long vesselId,
        [AizenRemoteCallBody] Aizen.Modules.Vessel.Abstraction.Request.Media.AddVesselMediaRequest request);

    [AizenRemoteCallPatch("/api/v1/vessels/{vesselId}/archive")]
    Task<AizenApiResponse<ArchiveVesselResponse>> ArchiveVessel(
        long vesselId,
        [AizenRemoteCallBody] ArchiveVesselRequest request);

    [AizenRemoteCallPatch("/api/v1/vessels/{vesselId}/restore")]
    Task<AizenApiResponse<RestoreVesselResponse>> RestoreVessel(long vesselId);

    [AizenRemoteCallPatch("/api/v1/vessels/{vesselId}/status")]
    Task<AizenApiResponse<UpdateVesselStatusResponse>> UpdateVesselStatus(
        long vesselId,
        [AizenRemoteCallBody] UpdateVesselStatusRequest request);

    [AizenRemoteCallGet("/api/v1/vessels/{vesselId}/media")]
    Task<AizenApiResponse<GetVesselMediaResponse>> GetVesselMedia(
        long vesselId,
        [Refit.Query] int  pageIndex                 = 0,
        [Refit.Query] int  pageSize                  = 20,
        [Refit.Query] bool includeAccessUrls         = false,
        [Refit.Query] int  accessUrlExpiresInMinutes = 60);

    [AizenRemoteCallGet("/api/v1/vessels/{vesselId}/status-history")]
    Task<AizenApiResponse<GetVesselStatusHistoryResponse>> GetVesselStatusHistory(
        long vesselId,
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize  = 20);

    [AizenRemoteCallGet("/api/v1/admin/vessels/counts-by-owner")]
    Task<AizenApiResponse<List<VesselCountByOwnerDto>>> GetVesselCountsByOwnerUserIds(
        [Refit.Query(CollectionFormat.Multi)] long[] userIds);

    [AizenRemoteCallGet("/api/v1/admin/vessels/names-by-ids")]
    Task<AizenApiResponse<List<VesselNameDto>>> GetVesselNamesByIds(
        [Refit.Query(CollectionFormat.Multi)] long[] vesselIds);

    [AizenRemoteCallGet("/api/v1/admin/vessels/status-history/by-owner")]
    Task<AizenApiResponse<GetVesselStatusHistoryByOwnerResponse>> GetVesselStatusHistoryByOwner(
        [Refit.Query] long ownerUserId,
        [Refit.Query] int  pageSize = 200);

    [AizenRemoteCallPost("/api/v1/admin/vessels")]
    Task<AizenApiResponse<CreateVesselResponse>> CreateAdminVessel(
        [AizenRemoteCallBody] CreateAdminVesselRequest request);
}
