using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Responses;

namespace Aizen.Modules.FileStorage.Abstraction.RemoteCall.File;

[DocumentationInfo("FileStorage remote call contract", "Defines synchronous server-to-server operations for FileStorage. Allows modules such as Vessel to validate files, link ownership and request temporary access URLs.")]
public interface IFileStorageRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/file-storage/files/{fileId}")]
    Task<GetFileMetadataRemoteCallResponse> GetFileMetadata(
        Guid fileId,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallPost("/api/v1/file-storage/files/{fileId}/validate-ownership")]
    Task<ValidateFileOwnershipRemoteCallResponse> ValidateFileOwnership(
        Guid fileId,
        [AizenRemoteCallBody] ValidateFileOwnershipRemoteCallRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallPost("/api/v1/file-storage/files/{fileId}/read-url")]
    Task<CreateFileReadUrlRemoteCallResponse> CreateReadUrl(
        Guid fileId,
        [AizenRemoteCallBody] CreateFileReadUrlRemoteCallRequest request,
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

    [AizenRemoteCallPost("/api/v1/file-storage/files/{fileId}/link-owner")]
    Task<LinkFileToOwnerRemoteCallResponse> LinkFileToOwner(
        Guid fileId,
        [AizenRemoteCallBody] LinkFileToOwnerRemoteCallRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);

    [AizenRemoteCallDelete("/api/v1/file-storage/files/{fileId}")]
    Task<DeleteFileRemoteCallResponse> DeleteFile(
        Guid fileId,
        [AizenRemoteCallBody] DeleteFileRemoteCallRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);
}
