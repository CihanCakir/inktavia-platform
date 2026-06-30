using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Enum;

namespace Aizen.Modules.FileStorage.Abstraction.Message;

[DocumentationInfo("File processing completed message", "Fire-and-forget message published when a background processing job completes.")]
public sealed class FileProcessingCompletedMessage : AizenBaseMessage
{
    public Guid FileId { get; set; }
    public FileProcessingType ProcessingType { get; set; }
    public bool IsSuccess { get; set; }
    public string? ResultDocumentId { get; set; }
    public string? ErrorCode { get; set; }
}
