using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.FileStorage.Consumers;

[DocumentationInfo("File metadata extraction requested consumer", "Fire-and-forget consumer that handles rich metadata extraction requests for files.")]
public sealed class FileMetadataExtractionRequestedConsumer
    : AizenBaseMessageConsumer<FileMetadataExtractionRequestedMessage>
{
    private readonly ILogger<FileMetadataExtractionRequestedConsumer> _logger;

    public FileMetadataExtractionRequestedConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<FileMetadataExtractionRequestedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(
        FileMetadataExtractionRequestedMessage message, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public override Task ExecuteCommitMessage(
        FileMetadataExtractionRequestedMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Metadata extraction requested: FileId={FileId}, ContentType={ContentType}, ObjectKey={ObjectKey}",
            message.FileId, message.ContentType, message.ObjectKey);
        return Task.CompletedTask;
    }

    public override Task ExecuteRollbackMessage(
        FileMetadataExtractionRequestedMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
