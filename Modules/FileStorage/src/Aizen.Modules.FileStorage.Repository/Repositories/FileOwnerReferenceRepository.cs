using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Domain.Entities.Access;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.FileStorage.Repository.Repositories;

[DocumentationInfo("File owner reference repository", "EF Core implementation of IFileOwnerReferenceRepository.")]
public sealed class FileOwnerReferenceRepository : IFileOwnerReferenceRepository
{
    private readonly FileStorageDbContext _db;

    public FileOwnerReferenceRepository(FileStorageDbContext db) => _db = db;

    public Task<FileOwnerReferenceEntity?> GetByFileAndOwnerAsync(long fileId, string ownerModule, string ownerEntityType, Guid ownerEntityId, CancellationToken ct = default)
        => _db.FileOwnerReferences.FirstOrDefaultAsync(x =>
            x.FileId == fileId &&
            x.OwnerModule == ownerModule &&
            x.OwnerEntityType == ownerEntityType &&
            x.OwnerEntityId == ownerEntityId, ct);

    public async Task<IReadOnlyList<FileOwnerReferenceEntity>> GetByFileIdAsync(long fileId, CancellationToken ct = default)
    {
        var result = await _db.FileOwnerReferences
            .Where(x => x.FileId == fileId)
            .ToListAsync(ct);
        return result;
    }

    public async Task<IReadOnlyList<FileOwnerReferenceEntity>> GetByOwnerAsync(string ownerModule, string ownerEntityType, Guid ownerEntityId, CancellationToken ct = default)
    {
        var result = await _db.FileOwnerReferences
            .Where(x => x.OwnerModule == ownerModule && x.OwnerEntityType == ownerEntityType && x.OwnerEntityId == ownerEntityId && x.IsActive)
            .ToListAsync(ct);
        return result;
    }

    public Task<bool> ExistsAsync(long fileId, string ownerModule, string ownerEntityType, Guid ownerEntityId, CancellationToken ct = default)
        => _db.FileOwnerReferences.AnyAsync(x =>
            x.FileId == fileId &&
            x.OwnerModule == ownerModule &&
            x.OwnerEntityType == ownerEntityType &&
            x.OwnerEntityId == ownerEntityId &&
            x.IsActive, ct);

    public Task AddAsync(FileOwnerReferenceEntity entity, CancellationToken ct = default)
        => _db.FileOwnerReferences.AddAsync(entity, ct).AsTask();

    public void Update(FileOwnerReferenceEntity entity) => _db.FileOwnerReferences.Update(entity);
}
