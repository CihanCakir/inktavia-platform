using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Document;
using Aizen.Modules.Vessel.Abstraction.Response.Ownership;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Vessel admin BFF remote call", "Defines synchronous BFF-to-Vessel calls for admin vessel management and inspection.")]
public interface IVesselAdminBffRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/admin/vessels")]
    Task<AizenApiResponse<GetAllVesselsAdminResponse>> GetAdminVesselList(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize = 20,
        [Refit.Query] string? searchTerm = null,
        [Refit.Query] bool? isArchived = null);

    [AizenRemoteCallGet("/api/v1/vessels/{vesselId}")]
    Task<AizenApiResponse<GetVesselDetailResponse>> GetVesselById(
        long vesselId,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallPut("/api/v1/vessels/{vesselId}")]
    Task<AizenApiResponse<UpdateVesselResponse>> UpdateVessel(
        long vesselId,
        [AizenRemoteCallBody] UpdateVesselRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallGet("/api/v1/vessels/{vesselId}/owners")]
    Task<AizenApiResponse<GetVesselOwnersResponse>> GetVesselOwners(
        long vesselId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize = 20);

    [AizenRemoteCallGet("/api/v1/vessels/{vesselId}/documents")]
    Task<AizenApiResponse<GetVesselDocumentsResponse>> GetVesselDocuments(
        long vesselId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize = 50,
        [Refit.Query] bool includeAccessUrls = false,
        [Refit.Query] int accessUrlExpiresInMinutes = 60);

    [AizenRemoteCallDelete("/api/v1/vessels/{vesselId}/documents/{documentId}")]
    Task<AizenApiResponse<RemoveVesselDocumentResponse>> RemoveVesselDocument(
        long vesselId,
        long documentId,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallPatch("/api/v1/vessels/{vesselId}/archive")]
    Task<AizenApiResponse<ArchiveVesselResponse>> ArchiveVessel(
        long vesselId,
        [AizenRemoteCallBody] ArchiveVesselRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallPatch("/api/v1/vessels/{vesselId}/restore")]
    Task<AizenApiResponse<RestoreVesselResponse>> RestoreVessel(
        long vesselId,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallPatch("/api/v1/vessels/{vesselId}/status")]
    Task<AizenApiResponse<UpdateVesselStatusResponse>> UpdateVesselStatus(
        long vesselId,
        [AizenRemoteCallBody] UpdateVesselStatusRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);
}
