using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Application.Queries.GetFilesByOwner;

[DocumentationInfo("Get files by owner query", "Returns all files linked to a specific owning module entity.")]
public sealed class GetFilesByOwnerQuery : AizenQuery<IReadOnlyList<FileDto>>
{
    public string OwnerModule { get; }
    public string OwnerEntityType { get; }
    public Guid OwnerEntityId { get; }

    public GetFilesByOwnerQuery(string ownerModule, string ownerEntityType, Guid ownerEntityId)
    {
        OwnerModule = ownerModule;
        OwnerEntityType = ownerEntityType;
        OwnerEntityId = ownerEntityId;
    }
}
