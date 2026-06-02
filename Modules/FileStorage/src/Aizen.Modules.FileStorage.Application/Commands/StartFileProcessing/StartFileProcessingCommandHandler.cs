using Aizen.Core.CQRS.Handler;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.FileStorage.Abstraction.Dto.Processing;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Commands.StartFileProcessing;

[DocumentationInfo("Start file processing command handler", "Delegates to IFileProcessingService to enqueue a job, then publishes FileProcessingRequestedMessage.")]
public sealed class StartFileProcessingCommandHandler : AizenCommandHandler<StartFileProcessingCommand, FileProcessingJobDto>
{
    private readonly IFileProcessingService _processingService;
    private readonly IFileRepository _fileRepository;
    private readonly IAizenMessagePublisher _publisher;

    public StartFileProcessingCommandHandler(
        IFileProcessingService processingService,
        IFileRepository fileRepository,
        IAizenMessagePublisher publisher)
    {
        _processingService = processingService;
        _fileRepository = fileRepository;
        _publisher = publisher;
    }

    public override async Task<FileProcessingJobDto?> Handle(StartFileProcessingCommand command, CancellationToken cancellationToken)
    {
        var file = await _fileRepository.GetByIdAsync(command.FileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File with id '{command.FileId}' not found.");

        var job = await _processingService.StartProcessingAsync(command.FileId, command.Request.ProcessingType, cancellationToken);

        await _publisher.PublishAsync(new FileProcessingRequestedMessage
        {
            FileId = file.PublicId ?? Guid.Empty,
            ObjectKey = file.ObjectKey,
            BucketName = file.BucketName,
            ProcessingType = command.Request.ProcessingType
        }, cancellationToken);

        return job;
    }
}
