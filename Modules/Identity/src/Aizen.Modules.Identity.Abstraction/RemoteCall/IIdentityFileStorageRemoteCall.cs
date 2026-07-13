using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Request.Access;
using Aizen.Modules.FileStorage.Abstraction.Request.File;

namespace Aizen.Modules.Identity.Abstraction.RemoteCall;

public interface IIdentityFileStorageRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/files/{fileId}")]
    Task<AizenApiResponse<FileDto>> GetFile(
        Guid fileId,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallPost("/api/v1/files/{fileId}/access/validate-ownership")]
    Task<AizenApiResponse<FileValidationResultDto>> ValidateOwnership(
        Guid fileId,
        [AizenRemoteCallBody] ValidateFileOwnershipRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallPost("/api/v1/files/{fileId}/owners")]
    Task<AizenApiResponse<FileOwnerReferenceDto>> LinkToOwner(
        Guid fileId,
        [AizenRemoteCallBody] LinkFileToOwnerRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallDelete("/api/v1/files/{fileId}")]
    Task<AizenApiResponse<IdentityFileDeleteResultDto>> DeleteFile(
        Guid fileId,
        [AizenRemoteCallBody] DeleteFileRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);
}

public sealed class IdentityFileDeleteResultDto
{
    public bool IsDeleted { get; set; }
}
