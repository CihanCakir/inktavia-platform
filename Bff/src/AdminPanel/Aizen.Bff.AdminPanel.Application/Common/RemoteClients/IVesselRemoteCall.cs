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

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Vessel admin BFF remote call",
    "Defines synchronous BFF-to-Vessel calls for admin vessel management and inspection. " +
    "Auth headers are injected automatically by AdminPanelBffAuthDelegatingHandler.")]
public interface IVesselRemoteCall : IAizenRemoteCall
{
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
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize  = 20);

    [AizenRemoteCallGet("/api/v1/vessels/{vesselId}/status-history")]
    Task<AizenApiResponse<GetVesselStatusHistoryResponse>> GetVesselStatusHistory(
        long vesselId,
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize  = 20);

    [AizenRemoteCallGet("/api/v1/admin/vessels/counts-by-owner")]
    Task<AizenApiResponse<List<VesselCountByOwnerDto>>> GetVesselCountsByOwnerUserIds(
        [Refit.Query(CollectionFormat.Multi)] long[] userIds);

    [AizenRemoteCallGet("/api/v1/admin/vessels/status-history/by-owner")]
    Task<AizenApiResponse<GetVesselStatusHistoryByOwnerResponse>> GetVesselStatusHistoryByOwner(
        [Refit.Query] long ownerUserId,
        [Refit.Query] int  pageSize = 200);

    [AizenRemoteCallPost("/api/v1/admin/vessels")]
    Task<AizenApiResponse<CreateVesselResponse>> CreateAdminVessel(
        [AizenRemoteCallBody] CreateAdminVesselRequest request);
}
