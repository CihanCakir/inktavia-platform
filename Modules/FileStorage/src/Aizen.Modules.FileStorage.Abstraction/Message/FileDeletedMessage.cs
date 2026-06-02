using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Message;

[DocumentationInfo("File deleted message", "Fire-and-forget message published after a file has been soft-deleted.")]
public sealed class FileDeletedMessage : AizenBaseMessage
{
    public Guid FileId { get; set; }
    public string ObjectKey { get; set; } = default!;
    public string BucketName { get; set; } = default!;
    public FileDeleteBehavior DeleteBehavior { get; set; }
    public long? DeletedByUserId { get; set; }
}
