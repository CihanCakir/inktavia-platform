using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("FileStorage admin BFF remote call", "Defines synchronous BFF-to-FileStorage calls for admin file inspection and access management.")]
public interface IFileStorageAdminBffRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/files/{fileId}")]
    Task<AizenApiResponse<FileMetadataResult>> GetFileMetadata(
        long fileId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPost("/api/v1/files/{fileId}/access/read-url")]
    Task<AizenApiResponse<FileAccessUrlResult>> CreateReadUrl(
        long fileId,
        [AizenRemoteCallBody] CreateFileReadUrlRemoteCallRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallDelete("/api/v1/files/{fileId}")]
    Task<AizenApiResponse<EmptyResult>> DeleteFile(
        long fileId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPatch("/api/v1/files/{fileId}/visibility")]
    Task<AizenApiResponse<EmptyResult>> UpdateFileVisibility(
        long fileId,
        [AizenRemoteCallBody] UpdateFileVisibilityRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);
}

public sealed class FileMetadataResult { public FileMetadataDto? File { get; set; } }
public sealed class FileAccessUrlResult { public FileAccessUrlDto? AccessUrl { get; set; } }

public sealed class UpdateFileVisibilityRequest
{
    public string Visibility { get; set; } = default!;
}
