using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.FileStorage.Abstraction.Message;

[DocumentationInfo("File linked to owner message", "Fire-and-forget message published when a file is linked to a module entity.")]
public sealed class FileLinkedToOwnerMessage : AizenBaseMessage
{
    public Guid FileId { get; set; }
    public string OwnerModule { get; set; } = default!;
    public string OwnerEntityType { get; set; } = default!;
    public Guid OwnerEntityId { get; set; }
}
