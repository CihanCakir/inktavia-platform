using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Request.Access;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Responses;

namespace Aizen.Modules.FileStorage.Abstraction.RemoteCall.File;

[DocumentationInfo("FileStorage remote call contract", "Defines synchronous server-to-server operations for FileStorage. Allows modules such as Vessel to validate files, link ownership and request temporary access URLs.")]
public interface IFileStorageRemoteCall : IAizenRemoteCall
{
    // The three methods the Vessel module actually uses (M4e) target file-storage-api's real public endpoints and
    // consume the standard AizenApiResponse envelope. (The original contract pointed at a never-built
    // `/api/v1/file-storage/*` prefix with bare response bodies, so these calls 404'd — vessel document attach had
    // never worked. This interface is consumed only by VesselFileStorageService.)
    [AizenRemoteCallGet("/api/v1/files/{fileId}/metadata")]
    Task<AizenApiResponse<FileMetadataDto>> GetFileMetadata(
        Guid fileId,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallPost("/api/v1/files/{fileId}/access/read-url")]
    Task<AizenApiResponse<FileAccessUrlDto>> CreateReadUrl(
        Guid fileId,
        [AizenRemoteCallBody] CreateReadUrlRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallPost("/api/v1/files/{fileId}/owners")]
    Task<AizenApiResponse<FileOwnerReferenceDto>> LinkFileToOwner(
        Guid fileId,
        [AizenRemoteCallBody] LinkFileToOwnerRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    // ── Not consumed by any module today; left as-is (still target the unbuilt S2S prefix). ──────────────
    [AizenRemoteCallPost("/api/v1/file-storage/files/{fileId}/validate-ownership")]
    Task<ValidateFileOwnershipRemoteCallResponse> ValidateFileOwnership(
        Guid fileId,
        [AizenRemoteCallBody] ValidateFileOwnershipRemoteCallRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallPost("/api/v1/file-storage/upload-sessions")]
    Task<CreateUploadSessionRemoteCallResponse> CreateUploadSession(
        [AizenRemoteCallBody] CreateUploadSessionRemoteCallRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallPost("/api/v1/file-storage/upload-sessions/{uploadSessionCode}/complete")]
    Task<CompleteUploadSessionRemoteCallResponse> CompleteUploadSession(
        string uploadSessionCode,
        [AizenRemoteCallBody] CompleteUploadSessionRemoteCallRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallDelete("/api/v1/file-storage/files/{fileId}")]
    Task<DeleteFileRemoteCallResponse> DeleteFile(
        Guid fileId,
        [AizenRemoteCallBody] DeleteFileRemoteCallRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);
}
