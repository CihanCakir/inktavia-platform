using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.FileStorage.Abstraction.Message;

[DocumentationInfo("Validate file ownership process message", "Request/response message to validate file ownership for an external module.")]
public sealed class ValidateFileOwnershipProcessMessage : AizenBaseMessage
{
    public Guid FileId { get; set; }
    public string OwnerModule { get; set; } = default!;
    public string OwnerEntityType { get; set; } = default!;
    public Guid OwnerEntityId { get; set; }
}

[DocumentationInfo("Validate file ownership process message result", "Response for the validate file ownership request/response message.")]
public sealed class ValidateFileOwnershipProcessMessageResult : AizenMessageResult
{
    public Guid FileId { get; set; }
    public bool IsValid { get; set; }
    public string? Reason { get; set; }
}
