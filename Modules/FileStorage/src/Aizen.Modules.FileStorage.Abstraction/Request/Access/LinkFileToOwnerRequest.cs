using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Request.Access;

[DocumentationInfo("Link file to owner request", "Links a file to an owning module entity.")]
public sealed class LinkFileToOwnerRequest
{
    public string OwnerModule { get; set; } = default!;
    public string OwnerEntityType { get; set; } = default!;
    public Guid OwnerEntityId { get; set; }
}
