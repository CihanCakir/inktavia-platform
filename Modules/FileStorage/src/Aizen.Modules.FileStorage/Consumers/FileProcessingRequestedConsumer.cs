using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.FileStorage.Consumers;

[DocumentationInfo("File processing requested consumer", "Fire-and-forget consumer that receives background processing requests for uploaded files.")]
public sealed class FileProcessingRequestedConsumer : AizenBaseMessageConsumer<FileProcessingRequestedMessage>
{
    private readonly ILogger<FileProcessingRequestedConsumer> _logger;

    public FileProcessingRequestedConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<FileProcessingRequestedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(
        FileProcessingRequestedMessage message, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public override Task ExecuteCommitMessage(
        FileProcessingRequestedMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "File processing requested: FileId={FileId}, ProcessingType={ProcessingType}, ObjectKey={ObjectKey}",
            message.FileId, message.ProcessingType, message.ObjectKey);
        return Task.CompletedTask;
    }

    public override Task ExecuteRollbackMessage(
        FileProcessingRequestedMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
