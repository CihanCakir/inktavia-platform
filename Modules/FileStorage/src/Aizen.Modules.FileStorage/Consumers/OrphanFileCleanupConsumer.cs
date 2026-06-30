using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.FileStorage.Consumers;

[DocumentationInfo("Orphan file cleanup consumer", "Fire-and-forget consumer that handles cleanup requests for files with no active owner references.")]
public sealed class OrphanFileCleanupConsumer : AizenBaseMessageConsumer<OrphanFileCleanupRequestedMessage>
{
    private readonly ILogger<OrphanFileCleanupConsumer> _logger;

    public OrphanFileCleanupConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<OrphanFileCleanupConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(
        OrphanFileCleanupRequestedMessage message, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public override Task ExecuteCommitMessage(
        OrphanFileCleanupRequestedMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Orphan file cleanup requested: FileId={FileId}, ObjectKey={ObjectKey}, Bucket={Bucket}",
            message.FileId, message.ObjectKey, message.BucketName);
        return Task.CompletedTask;
    }

    public override Task ExecuteRollbackMessage(
        OrphanFileCleanupRequestedMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
