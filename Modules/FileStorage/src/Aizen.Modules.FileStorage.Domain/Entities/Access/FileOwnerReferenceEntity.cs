using Aizen.Core.Domain;
using Aizen.Modules.FileStorage.Domain.Entities.File;

namespace Aizen.Modules.FileStorage.Domain.Entities.Access;

[DocumentationInfo("File owner reference entity", "Links a file to an owning module entity, tracking which external domain owns the file.")]
public sealed class FileOwnerReferenceEntity : AizenEntityWithAudit
{
    public long FileId { get; private set; }
    public string OwnerModule { get; private set; } = default!;
    public string OwnerEntityType { get; private set; } = default!;
    public Guid OwnerEntityId { get; private set; }
    public DateTime LinkedAt { get; private set; }
    public long? LinkedByUserId { get; private set; }

    public FileEntity? File { get; private set; }

    public FileOwnerReferenceEntity() { }

    public static FileOwnerReferenceEntity Create(
        long fileId, string ownerModule, string ownerEntityType,
        Guid ownerEntityId, long? linkedByUserId)
    {
        return new FileOwnerReferenceEntity
        {
            FileId = fileId,
            OwnerModule = ownerModule.ToUpperInvariant(),
            OwnerEntityType = ownerEntityType,
            OwnerEntityId = ownerEntityId,
            LinkedAt = DateTime.UtcNow,
            LinkedByUserId = linkedByUserId,
            IsActive = true
        };
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
