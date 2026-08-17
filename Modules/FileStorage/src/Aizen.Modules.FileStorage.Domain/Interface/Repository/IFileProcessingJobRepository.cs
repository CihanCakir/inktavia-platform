using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Domain.Entities.Processing;

namespace Aizen.Modules.FileStorage.Domain.Interface.Repository;

[DocumentationInfo("File processing job repository interface", "Data access contract for background file processing jobs.")]
public interface IFileProcessingJobRepository
{
    Task<FileProcessingJobEntity?> GetByFileAndTypeAsync(long fileId, FileProcessingType processingType, CancellationToken ct = default);
    Task<IReadOnlyList<FileProcessingJobEntity>> GetByFileIdAsync(long fileId, CancellationToken ct = default);
    Task<IReadOnlyList<FileProcessingJobEntity>> GetPendingAsync(CancellationToken ct = default);
    Task AddAsync(FileProcessingJobEntity entity, CancellationToken ct = default);
    void Update(FileProcessingJobEntity entity);
}
