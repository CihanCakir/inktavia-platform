using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Message;

[DocumentationInfo("File metadata extraction requested message", "Fire-and-forget message requesting rich metadata extraction for a file.")]
public sealed class FileMetadataExtractionRequestedMessage : AizenBaseMessage
{
    public Guid FileId { get; set; }
    public string ObjectKey { get; set; } = default!;
    public string BucketName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
}
