using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.FileStorage.Abstraction.Dto.UploadSession;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Aizen.Modules.FileStorage.Abstraction.Request.Access;

namespace Aizen.Bff.MarineProvider.Application.Common.RemoteClients;

/// <summary>
/// BFF -> FileStorage module calls. Auth (Keycloak service token + BFF assertion headers)
/// is injected automatically by <c>MarineProviderBffAuthDelegatingHandler</c>.
/// </summary>
public interface IProviderFileStorageRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallPost("/api/v1/upload-sessions")]
    Task<AizenApiResponse<FileUploadSessionDto>> CreateUploadSession(
        [AizenRemoteCallBody] CreateUploadSessionRequest request);

    [AizenRemoteCallPost("/api/v1/upload-sessions/{uploadSessionCode}/complete")]
    Task<AizenApiResponse<FileDto>> CompleteUploadSession(
        string uploadSessionCode,
        [AizenRemoteCallBody] CompleteUploadSessionRequest request);

    [AizenRemoteCallGet("/api/v1/files/{fileId}")]
    Task<AizenApiResponse<FileMetadataDto>> GetFileMetadata(Guid fileId);

    [AizenRemoteCallPost("/api/v1/files/{fileId}/access/read-url")]
    Task<AizenApiResponse<FileAccessUrlDto>> CreateReadUrl(
        Guid fileId,
        [AizenRemoteCallBody] CreateReadUrlRequest request);

    [AizenRemoteCallDelete("/api/v1/files/{fileId}")]
    Task<AizenApiResponse<FileDeleteResultDto>> DeleteFile(
        Guid fileId,
        [AizenRemoteCallBody] DeleteFileRequest request);

    [AizenRemoteCallPost("/api/v1/files/{fileId}/access/validate-ownership")]
    Task<AizenApiResponse<FileValidationResultDto>> ValidateOwnership(
        Guid fileId,
        [AizenRemoteCallBody] ValidateFileOwnershipRequest request);

    [AizenRemoteCallPost("/api/v1/files/{fileId}/owners")]
    Task<AizenApiResponse<FileOwnerReferenceDto>> LinkToOwner(
        Guid fileId,
        [AizenRemoteCallBody] LinkFileToOwnerRequest request);
}

public sealed class FileDeleteResultDto
{
    public bool IsDeleted { get; set; }
}
