using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Dto.UploadSession;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;

/// <summary>
/// BFF → FileStorage module calls (Keycloak service token injected by the delegating handler; the mobile BFF
/// service account holds file_storage_read/write). Used by the canonical client-side presigned flow: create a
/// session (ServerSideUpload=false → presigned PUT signed for the device-reachable public endpoint), which the
/// CLIENT PUTs bytes to directly; complete it; then mint a presigned read URL for rendering.
/// </summary>
public interface IFileStorageRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallPost("/api/v1/upload-sessions")]
    Task<AizenApiResponse<FileUploadSessionDto>> CreateUploadSession(
        [AizenRemoteCallBody] CreateUploadSessionRequest request);

    [AizenRemoteCallPost("/api/v1/upload-sessions/{uploadSessionCode}/complete")]
    Task<AizenApiResponse<FileDto>> CompleteUploadSession(
        string uploadSessionCode,
        [AizenRemoteCallBody] CompleteUploadSessionRequest request);

    [AizenRemoteCallPost("/api/v1/files/{fileId}/access/read-url")]
    Task<AizenApiResponse<FileAccessUrlDto>> CreateReadUrl(
        Guid fileId,
        [AizenRemoteCallBody] CreateReadUrlRequest request);
}
