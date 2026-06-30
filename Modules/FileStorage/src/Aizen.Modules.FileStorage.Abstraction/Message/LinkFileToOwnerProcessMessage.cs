using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.FileStorage.Abstraction.Message;

[DocumentationInfo("Link file to owner process message", "Request/response message to link a file to an owning module entity.")]
public sealed class LinkFileToOwnerProcessMessage : AizenBaseMessage
{
    public Guid FileId { get; set; }
    public string OwnerModule { get; set; } = default!;
    public string OwnerEntityType { get; set; } = default!;
    public Guid OwnerEntityId { get; set; }
    public long LinkedByUserId { get; set; }
}

[DocumentationInfo("Link file to owner process message result", "Response for the link file to owner request/response message.")]
public sealed class LinkFileToOwnerProcessMessageResult : AizenMessageResult
{
    public Guid FileId { get; set; }
    public bool IsLinked { get; set; }
}
