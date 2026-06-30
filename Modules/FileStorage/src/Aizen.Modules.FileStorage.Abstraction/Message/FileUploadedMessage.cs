using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.FileStorage.Abstraction.Message;

[DocumentationInfo("File uploaded message", "Fire-and-forget message published when a file upload is completed and validated.")]
public sealed class FileUploadedMessage : AizenBaseMessage
{
    public Guid FileId { get; set; }
    public string FileCode { get; set; } = default!;
    public string ObjectKey { get; set; } = default!;
    public string BucketName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long SizeInBytes { get; set; }
    public long? UploadedByUserId { get; set; }
}
