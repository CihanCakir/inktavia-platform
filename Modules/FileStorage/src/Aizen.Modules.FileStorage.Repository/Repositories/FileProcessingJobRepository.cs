using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Domain.Entities.Processing;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.FileStorage.Repository.Repositories;

[DocumentationInfo("File processing job repository", "EF Core implementation of IFileProcessingJobRepository.")]
public sealed class FileProcessingJobRepository : IFileProcessingJobRepository
{
    private readonly FileStorageDbContext _db;

    public FileProcessingJobRepository(FileStorageDbContext db) => _db = db;

    public Task<FileProcessingJobEntity?> GetByFileAndTypeAsync(long fileId, FileProcessingType processingType, CancellationToken ct = default)
        => _db.FileProcessingJobs.FirstOrDefaultAsync(x => x.FileId == fileId && x.ProcessingType == processingType, ct);

    public async Task<IReadOnlyList<FileProcessingJobEntity>> GetByFileIdAsync(long fileId, CancellationToken ct = default)
    {
        var result = await _db.FileProcessingJobs
            .Where(x => x.FileId == fileId)
            .ToListAsync(ct);
        return result;
    }

    public async Task<IReadOnlyList<FileProcessingJobEntity>> GetPendingAsync(CancellationToken ct = default)
    {
        var result = await _db.FileProcessingJobs
            .Where(x => x.Status == FileProcessingStatus.Pending)
            .ToListAsync(ct);
        return result;
    }

    public Task AddAsync(FileProcessingJobEntity entity, CancellationToken ct = default)
        => _db.FileProcessingJobs.AddAsync(entity, ct).AsTask();

    public void Update(FileProcessingJobEntity entity) => _db.FileProcessingJobs.Update(entity);
}
