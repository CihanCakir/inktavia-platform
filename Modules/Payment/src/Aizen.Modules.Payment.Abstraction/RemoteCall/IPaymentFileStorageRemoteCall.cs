using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Dto.UploadSession;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;

namespace Aizen.Modules.Payment.Abstraction.RemoteCall;

public interface IPaymentFileStorageRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallPost("/api/v1/upload-sessions")]
    Task<AizenApiResponse<FileUploadSessionDto>> CreateUploadSession(
        [AizenRemoteCallBody] CreateUploadSessionRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallPost("/api/v1/upload-sessions/{uploadSessionCode}/complete")]
    Task<AizenApiResponse<FileDto>> CompleteUploadSession(
        string uploadSessionCode,
        [AizenRemoteCallBody] CompleteUploadSessionRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallPost("/api/v1/files/{fileId}/access/read-url")]
    Task<AizenApiResponse<FileAccessUrlDto>> CreateReadUrl(
        Guid fileId,
        [AizenRemoteCallBody] CreateReadUrlRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);
}
