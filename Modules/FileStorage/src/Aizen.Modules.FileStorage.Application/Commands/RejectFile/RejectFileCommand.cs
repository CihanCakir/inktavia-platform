using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.FileStorage.Application.Commands.RejectFile;

[DocumentationInfo("Reject file command", "Marks a file as rejected (e.g., failed virus scan).")]
public sealed class RejectFileCommand : AizenCommand<bool>
{
    public long FileId { get; set; }
    public string Reason { get; set; } = default!;
}
