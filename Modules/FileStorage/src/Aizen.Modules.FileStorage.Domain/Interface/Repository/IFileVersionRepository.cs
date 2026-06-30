using Aizen.Modules.FileStorage.Domain.Entities.File;

namespace Aizen.Modules.FileStorage.Domain.Interface.Repository;

[DocumentationInfo("File version repository interface", "Data access contract for file version history records.")]
public interface IFileVersionRepository
{
    Task<IReadOnlyList<FileVersionEntity>> GetByFileIdAsync(long fileId, CancellationToken ct = default);
    Task<FileVersionEntity?> GetLatestAsync(long fileId, CancellationToken ct = default);
    Task AddAsync(FileVersionEntity entity, CancellationToken ct = default);
}
