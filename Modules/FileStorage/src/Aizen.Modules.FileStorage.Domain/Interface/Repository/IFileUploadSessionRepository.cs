using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Domain.Entities.UploadSession;

namespace Aizen.Modules.FileStorage.Domain.Interface.Repository;

[DocumentationInfo("File upload session repository interface", "Data access contract for S3 pre-signed upload sessions.")]
public interface IFileUploadSessionRepository
{
    Task<FileUploadSessionEntity?> GetByCodeAsync(string uploadSessionCode, CancellationToken ct = default);
    Task<FileUploadSessionEntity?> GetByFileIdAsync(long fileId, CancellationToken ct = default);
    Task AddAsync(FileUploadSessionEntity entity, CancellationToken ct = default);
    void Update(FileUploadSessionEntity entity);
}
