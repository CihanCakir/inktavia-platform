using Aizen.Modules.FileStorage.Abstraction.Dto.UploadSession;
using Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;

namespace Aizen.Modules.FileStorage.Domain.Interface.Service;

[DocumentationInfo("File upload session service interface", "Creates FileEntity, FileUploadSessionEntity, generates S3 object key and pre-signed upload URL.")]
public interface IFileUploadSessionService
{
    Task<FileUploadSessionDto> CreateUploadSessionAsync(
        CreateUploadSessionRequest request,
        long? userId, string? clientId, string? deviceId,
        CancellationToken cancellationToken = default);
}
