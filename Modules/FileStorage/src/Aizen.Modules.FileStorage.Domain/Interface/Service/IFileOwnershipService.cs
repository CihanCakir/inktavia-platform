using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Abstraction.Request.Access;

namespace Aizen.Modules.FileStorage.Domain.Interface.Service;

[DocumentationInfo("File ownership service interface", "Links files to owning module entities and validates ownership claims.")]
public interface IFileOwnershipService
{
    Task<FileOwnerReferenceDto> LinkFileToOwnerAsync(long fileId, LinkFileToOwnerRequest request, long? linkedByUserId, CancellationToken cancellationToken = default);
    Task<bool> ValidateOwnershipAsync(long fileId, string ownerModule, string ownerEntityType, Guid ownerEntityId, CancellationToken cancellationToken = default);
}
