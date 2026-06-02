using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Domain.Entities.File;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.FileStorage.Repository.Repositories;

[DocumentationInfo("File repository", "EF Core implementation of IFileRepository for the file root aggregate.")]
public sealed class FileRepository : IFileRepository
{
    private readonly FileStorageDbContext _db;

    public FileRepository(FileStorageDbContext db) => _db = db;

    public Task<FileEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.Files.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<FileEntity?> GetByFileCodeAsync(string fileCode, CancellationToken ct = default)
        => _db.Files.FirstOrDefaultAsync(x => x.FileCode == fileCode && !x.IsDeleted, ct);

    public Task<FileEntity?> GetByGuidAsync(Guid fileId, CancellationToken ct = default)
        => _db.Files.FirstOrDefaultAsync(x => x.PublicId == fileId && !x.IsDeleted, ct);

    public Task<FileEntity?> GetByObjectKeyAsync(string bucketName, string objectKey, CancellationToken ct = default)
        => _db.Files.FirstOrDefaultAsync(x => x.BucketName == bucketName && x.ObjectKey == objectKey && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<FileEntity>> GetByOwnerAsync(string ownerModule, string ownerEntityType, Guid ownerEntityId, CancellationToken ct = default)
    {
        var result = await _db.Files
            .Where(f => !f.IsDeleted && f.OwnerReferences.Any(r =>
                r.OwnerModule == ownerModule &&
                r.OwnerEntityType == ownerEntityType &&
                r.OwnerEntityId == ownerEntityId &&
                r.IsActive))
            .ToListAsync(ct);
        return result;
    }

    public Task<bool> ExistsByFileCodeAsync(string fileCode, CancellationToken ct = default)
        => _db.Files.AnyAsync(x => x.FileCode == fileCode, ct);

    public Task AddAsync(FileEntity entity, CancellationToken ct = default)
        => _db.Files.AddAsync(entity, ct).AsTask();

    public void Update(FileEntity entity) => _db.Files.Update(entity);
}
