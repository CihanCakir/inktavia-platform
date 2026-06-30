using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Enum;

namespace Aizen.Modules.FileStorage.Abstraction.Message;

[DocumentationInfo("Delete file process message", "Request/response message to delete a file (soft-delete + optional S3 removal).")]
public sealed class DeleteFileProcessMessage : AizenBaseMessage
{
    public Guid FileId { get; set; }
    public FileDeleteBehavior DeleteBehavior { get; set; } = FileDeleteBehavior.SoftDeleteOnly;
    public long DeletedByUserId { get; set; }
}

[DocumentationInfo("Delete file process message result", "Response for the delete file request/response message.")]
public sealed class DeleteFileProcessMessageResult : AizenMessageResult
{
    public Guid FileId { get; set; }
    public bool IsDeleted { get; set; }
}
