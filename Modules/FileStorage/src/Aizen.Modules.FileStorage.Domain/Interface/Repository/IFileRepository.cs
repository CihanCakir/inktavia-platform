using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Domain.Entities.File;

namespace Aizen.Modules.FileStorage.Domain.Interface.Repository;

[DocumentationInfo("File repository interface", "Data access contract for the file root aggregate.")]
public interface IFileRepository
{
    Task<FileEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<FileEntity?> GetByFileCodeAsync(string fileCode, CancellationToken ct = default);
    Task<FileEntity?> GetByGuidAsync(Guid fileId, CancellationToken ct = default);
    Task<FileEntity?> GetByObjectKeyAsync(string bucketName, string objectKey, CancellationToken ct = default);
    Task<IReadOnlyList<FileEntity>> GetByOwnerAsync(string ownerModule, string ownerEntityType, Guid ownerEntityId, CancellationToken ct = default);
    Task<bool> ExistsByFileCodeAsync(string fileCode, CancellationToken ct = default);
    Task AddAsync(FileEntity entity, CancellationToken ct = default);
    void Update(FileEntity entity);
}
