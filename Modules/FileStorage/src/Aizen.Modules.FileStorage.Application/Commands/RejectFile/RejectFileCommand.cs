using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Application.Commands.RejectFile;

[DocumentationInfo("Reject file command", "Marks a file as rejected (e.g., failed virus scan).")]
public sealed class RejectFileCommand : AizenCommand<bool>
{
    public long FileId { get; set; }
    public string Reason { get; set; } = default!;
}
