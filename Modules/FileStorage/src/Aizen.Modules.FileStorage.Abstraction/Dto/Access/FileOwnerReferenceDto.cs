using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Dto.Access;

[DocumentationInfo("File owner reference DTO", "Represents a link between a file and an owning module entity.")]
public sealed class FileOwnerReferenceDto
{
    public Guid FileId { get; set; }
    public string OwnerModule { get; set; } = default!;
    public string OwnerEntityType { get; set; } = default!;
    public Guid OwnerEntityId { get; set; }
    public DateTime LinkedAt { get; set; }
    public long? LinkedByUserId { get; set; }
    public bool IsActive { get; set; }
}
