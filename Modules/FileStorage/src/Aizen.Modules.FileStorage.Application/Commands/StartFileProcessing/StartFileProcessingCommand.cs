using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Dto.Processing;
using Aizen.Modules.FileStorage.Abstraction.Request.Processing;

namespace Aizen.Modules.FileStorage.Application.Commands.StartFileProcessing;

[DocumentationInfo("Start file processing command", "Enqueues a background processing job for a file.")]
public sealed class StartFileProcessingCommand : AizenCommand<FileProcessingJobDto>
{
    public Guid FileId { get; set; }
    public StartFileProcessingRequest Request { get; set; } = default!;
}
