using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.FileStorage.Consumers;

[DocumentationInfo("File virus scan requested consumer", "Fire-and-forget consumer that handles virus scan requests for uploaded files.")]
public sealed class FileVirusScanRequestedConsumer : AizenBaseMessageConsumer<FileVirusScanRequestedMessage>
{
    private readonly ILogger<FileVirusScanRequestedConsumer> _logger;

    public FileVirusScanRequestedConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<FileVirusScanRequestedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(
        FileVirusScanRequestedMessage message, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public override Task ExecuteCommitMessage(
        FileVirusScanRequestedMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Virus scan requested: FileId={FileId}, ObjectKey={ObjectKey}, Bucket={Bucket}",
            message.FileId, message.ObjectKey, message.BucketName);
        return Task.CompletedTask;
    }

    public override Task ExecuteRollbackMessage(
        FileVirusScanRequestedMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
