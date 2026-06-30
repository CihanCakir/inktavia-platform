using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.FileStorage.Consumers;

[DocumentationInfo("File uploaded consumer", "Fire-and-forget consumer that handles FileUploadedMessage to trigger downstream processing.")]
public sealed class FileUploadedConsumer : AizenBaseMessageConsumer<FileUploadedMessage>
{
    private readonly ILogger<FileUploadedConsumer> _logger;

    public FileUploadedConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<FileUploadedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(
        FileUploadedMessage message, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public override Task ExecuteCommitMessage(
        FileUploadedMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "File uploaded: FileId={FileId}, ObjectKey={ObjectKey}, Bucket={Bucket}, Size={Size}",
            message.FileId, message.ObjectKey, message.BucketName, message.SizeInBytes);
        return Task.CompletedTask;
    }

    public override Task ExecuteRollbackMessage(
        FileUploadedMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
