using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Abstraction.Request.Processing;

namespace Aizen.Modules.FileStorage.Application.Commands.UpdateFileProcessingResult;

[DocumentationInfo("Update file processing result command", "Records the outcome of a completed processing job.")]
public sealed class UpdateFileProcessingResultCommand : AizenCommand<bool>
{
    public long FileId { get; set; }
    public UpdateFileProcessingResultRequest Request { get; set; } = default!;
}
