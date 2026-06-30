using Aizen.Modules.FileStorage.Domain.Entities.Access;

namespace Aizen.Modules.FileStorage.Domain.Interface.Repository;

[DocumentationInfo("File access policy repository interface", "Data access contract for file visibility and access control policies.")]
public interface IFileAccessPolicyRepository
{
    Task<FileAccessPolicyEntity?> GetByFileIdAsync(long fileId, CancellationToken ct = default);
    Task AddAsync(FileAccessPolicyEntity entity, CancellationToken ct = default);
    void Update(FileAccessPolicyEntity entity);
}
