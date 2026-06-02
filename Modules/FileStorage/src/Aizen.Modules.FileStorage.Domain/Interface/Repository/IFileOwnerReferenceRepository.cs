using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Domain.Entities.Access;

namespace Aizen.Modules.FileStorage.Domain.Interface.Repository;

[DocumentationInfo("File owner reference repository interface", "Data access contract for file-to-module owner links.")]
public interface IFileOwnerReferenceRepository
{
    Task<FileOwnerReferenceEntity?> GetByFileAndOwnerAsync(long fileId, string ownerModule, string ownerEntityType, Guid ownerEntityId, CancellationToken ct = default);
    Task<IReadOnlyList<FileOwnerReferenceEntity>> GetByFileIdAsync(long fileId, CancellationToken ct = default);
    Task<IReadOnlyList<FileOwnerReferenceEntity>> GetByOwnerAsync(string ownerModule, string ownerEntityType, Guid ownerEntityId, CancellationToken ct = default);
    Task<bool> ExistsAsync(long fileId, string ownerModule, string ownerEntityType, Guid ownerEntityId, CancellationToken ct = default);
    Task AddAsync(FileOwnerReferenceEntity entity, CancellationToken ct = default);
    void Update(FileOwnerReferenceEntity entity);
}
