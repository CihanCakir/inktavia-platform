using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.FileStorage.Abstraction.Message;

[DocumentationInfo("File thumbnail requested message", "Fire-and-forget message requesting thumbnail generation for an image file.")]
public sealed class FileThumbnailRequestedMessage : AizenBaseMessage
{
    public Guid FileId { get; set; }
    public string ObjectKey { get; set; } = default!;
    public string BucketName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
}
