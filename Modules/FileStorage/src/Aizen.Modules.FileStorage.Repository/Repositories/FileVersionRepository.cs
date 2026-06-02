using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Domain.Entities.File;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.FileStorage.Repository.Repositories;

[DocumentationInfo("File version repository", "EF Core implementation of IFileVersionRepository.")]
public sealed class FileVersionRepository : IFileVersionRepository
{
    private readonly FileStorageDbContext _db;

    public FileVersionRepository(FileStorageDbContext db) => _db = db;

    public async Task<IReadOnlyList<FileVersionEntity>> GetByFileIdAsync(long fileId, CancellationToken ct = default)
    {
        var result = await _db.FileVersions
            .Where(x => x.FileId == fileId)
            .OrderBy(x => x.VersionNo)
            .ToListAsync(ct);
        return result;
    }

    public Task<FileVersionEntity?> GetLatestAsync(long fileId, CancellationToken ct = default)
        => _db.FileVersions
            .Where(x => x.FileId == fileId)
            .OrderByDescending(x => x.VersionNo)
            .FirstOrDefaultAsync(ct);

    public Task AddAsync(FileVersionEntity entity, CancellationToken ct = default)
        => _db.FileVersions.AddAsync(entity, ct).AsTask();
}
