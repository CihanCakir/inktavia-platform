using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Domain.Entities.UploadSession;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.FileStorage.Repository.Repositories;

[DocumentationInfo("File upload session repository", "EF Core implementation of IFileUploadSessionRepository.")]
public sealed class FileUploadSessionRepository : IFileUploadSessionRepository
{
    private readonly FileStorageDbContext _db;

    public FileUploadSessionRepository(FileStorageDbContext db) => _db = db;

    public Task<FileUploadSessionEntity?> GetByCodeAsync(string uploadSessionCode, CancellationToken ct = default)
        => _db.FileUploadSessions.FirstOrDefaultAsync(x => x.UploadSessionCode == uploadSessionCode, ct);

    public Task<FileUploadSessionEntity?> GetByFileIdAsync(long fileId, CancellationToken ct = default)
        => _db.FileUploadSessions
            .Where(x => x.FileId == fileId)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

    public Task AddAsync(FileUploadSessionEntity entity, CancellationToken ct = default)
        => _db.FileUploadSessions.AddAsync(entity, ct).AsTask();

    public void Update(FileUploadSessionEntity entity) => _db.FileUploadSessions.Update(entity);
}
