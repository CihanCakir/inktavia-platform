using Aizen.Modules.FileStorage.Domain.Entities.Access;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.FileStorage.Repository.Repositories;

[DocumentationInfo("File access policy repository", "EF Core implementation of IFileAccessPolicyRepository.")]
public sealed class FileAccessPolicyRepository : IFileAccessPolicyRepository
{
    private readonly FileStorageDbContext _db;

    public FileAccessPolicyRepository(FileStorageDbContext db) => _db = db;

    public Task<FileAccessPolicyEntity?> GetByFileIdAsync(long fileId, CancellationToken ct = default)
        => _db.FileAccessPolicies.FirstOrDefaultAsync(x => x.FileId == fileId && x.IsActive, ct);

    public Task AddAsync(FileAccessPolicyEntity entity, CancellationToken ct = default)
        => _db.FileAccessPolicies.AddAsync(entity, ct).AsTask();

    public void Update(FileAccessPolicyEntity entity) => _db.FileAccessPolicies.Update(entity);
}
