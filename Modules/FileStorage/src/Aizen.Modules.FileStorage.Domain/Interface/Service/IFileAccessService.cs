using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Domain.Interface.Service;

[DocumentationInfo("File access service interface", "Validates file access rights and generates pre-signed read URLs.")]
public interface IFileAccessService
{
    Task<FileAccessUrlDto> CreateReadUrlAsync(long fileId, TimeSpan expiresIn, long? requestedByUserId, CancellationToken cancellationToken = default);
    Task EnsureCanReadAsync(long fileId, long? requestedByUserId, CancellationToken cancellationToken = default);
}
