using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.FileStorage.Consumers;

[DocumentationInfo("File uploaded consumer", "Fire-and-forget consumer that handles FileUploadedMessage to trigger downstream processing (virus scan, metadata extraction, thumbnails).")]
public sealed class FileUploadedConsumer : AizenBaseMessageConsumer<FileUploadedMessage>
{
    private readonly ILogger<FileUploadedConsumer> _logger;
    private readonly IAizenMessagePublisher _publisher;

    public FileUploadedConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<FileUploadedConsumer>>();
        _publisher = serviceProvider.GetRequiredService<IAizenMessagePublisher>();
    }

    public override Task<bool> ExecutePrepareMessage(
        FileUploadedMessage message, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(
        FileUploadedMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "File uploaded: FileId={FileId}, ObjectKey={ObjectKey}, Bucket={Bucket}, Size={Size}. Triggering virus scan.",
            message.FileId, message.ObjectKey, message.BucketName, message.SizeInBytes);

        // Trigger the virus/malware scan pipeline. The scan consumer will promote
        // the file to Ready (clean) or Quarantined (threat detected).
        await _publisher.PublishAsync(new FileVirusScanRequestedMessage
        {
            FileId = message.FileId,
            ObjectKey = message.ObjectKey,
            BucketName = message.BucketName
        }, cancellationToken);
    }

    public override Task ExecuteRollbackMessage(
        FileUploadedMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
