using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.FileStorage.Consumers;

[DocumentationInfo("File processing completed consumer", "Fire-and-forget consumer that handles FileProcessingCompletedMessage to record job outcomes.")]
public sealed class FileProcessingCompletedConsumer : AizenBaseMessageConsumer<FileProcessingCompletedMessage>
{
    private readonly ILogger<FileProcessingCompletedConsumer> _logger;

    public FileProcessingCompletedConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<FileProcessingCompletedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(
        FileProcessingCompletedMessage message, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public override Task ExecuteCommitMessage(
        FileProcessingCompletedMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "File processing completed: FileId={FileId}, ProcessingType={ProcessingType}, Success={IsSuccess}, ResultDocumentId={ResultDocumentId}",
            message.FileId, message.ProcessingType, message.IsSuccess, message.ResultDocumentId);
        return Task.CompletedTask;
    }

    public override Task ExecuteRollbackMessage(
        FileProcessingCompletedMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
