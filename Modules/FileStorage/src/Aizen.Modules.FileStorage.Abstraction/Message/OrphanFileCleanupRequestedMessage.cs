using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Message;

[DocumentationInfo("Orphan file cleanup requested message", "Fire-and-forget message requesting cleanup of files with no active owner references.")]
public sealed class OrphanFileCleanupRequestedMessage : AizenBaseMessage
{
    public Guid FileId { get; set; }
    public string ObjectKey { get; set; } = default!;
    public string BucketName { get; set; } = default!;
}
