using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Message;

[DocumentationInfo("File virus scan requested message", "Fire-and-forget message requesting a virus scan for an uploaded file.")]
public sealed class FileVirusScanRequestedMessage : AizenBaseMessage
{
    public Guid FileId { get; set; }
    public string ObjectKey { get; set; } = default!;
    public string BucketName { get; set; } = default!;
}
