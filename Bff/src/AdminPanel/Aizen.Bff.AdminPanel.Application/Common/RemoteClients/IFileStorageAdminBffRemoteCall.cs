using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("FileStorage admin BFF remote call",
    "Defines synchronous BFF-to-FileStorage calls for admin file inspection and access management. " +
    "Auth headers are injected automatically by AdminPanelBffAuthDelegatingHandler.")]
public interface IFileStorageAdminBffRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/files/{fileId}")]
    Task<AizenApiResponse<FileMetadataResult>> GetFileMetadata(Guid fileId);

    [AizenRemoteCallPost("/api/v1/files/{fileId}/access/read-url")]
    Task<AizenApiResponse<FileAccessUrlResult>> CreateReadUrl(
        Guid fileId,
        [AizenRemoteCallBody] CreateFileReadUrlRemoteCallRequest request);

    [AizenRemoteCallDelete("/api/v1/files/{fileId}")]
    Task<AizenApiResponse<EmptyResult>> DeleteFile(Guid fileId);

    [AizenRemoteCallPatch("/api/v1/files/{fileId}/visibility")]
    Task<AizenApiResponse<EmptyResult>> UpdateFileVisibility(
        Guid fileId,
        [AizenRemoteCallBody] UpdateFileVisibilityRequest request);

    /// <summary>
    /// Request a pre-signed PUT URL so the browser can upload a verification document directly to MinIO/S3.
    /// </summary>
    [AizenRemoteCallPost("/api/v1/file-storage/upload-sessions")]
    Task<AizenApiResponse<CreateDocumentUploadSessionResult>> CreateDocumentUploadSession(
        [AizenRemoteCallBody] CreateDocumentUploadSessionRequest request);

    /// <summary>
    /// Complete an upload session after the browser has PUT the file to MinIO/S3.
    /// </summary>
    [AizenRemoteCallPost("/api/v1/file-storage/upload-sessions/{uploadSessionCode}/complete")]
    Task<AizenApiResponse<CompleteDocumentUploadSessionResult>> CompleteDocumentUploadSession(
        string uploadSessionCode,
        [AizenRemoteCallBody] CompleteDocumentUploadSessionRequest request);
}

public sealed class FileMetadataResult { public FileMetadataDto? File { get; set; } }
public sealed class FileAccessUrlResult { public FileAccessUrlDto? AccessUrl { get; set; } }

public sealed class UpdateFileVisibilityRequest
{
    public string Visibility { get; set; } = default!;
}

public sealed class CreateDocumentUploadSessionRequest
{
    public string OriginalFileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long SizeInBytes { get; set; }
    public string? OwnerModule { get; set; }
}

public sealed class CreateDocumentUploadSessionResult
{
    public Guid FileId { get; set; }
    public string UploadSessionCode { get; set; } = default!;
    public string UploadUrl { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
}

public sealed class CompleteDocumentUploadSessionRequest
{
    public string? Checksum { get; set; }
}

public sealed class CompleteDocumentUploadSessionResult
{
    public Guid FileId { get; set; }
    public string Status { get; set; } = default!;
}
