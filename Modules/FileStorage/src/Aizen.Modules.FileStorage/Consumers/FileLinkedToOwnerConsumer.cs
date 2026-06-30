using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.FileStorage.Consumers;

[DocumentationInfo("File linked to owner consumer", "Fire-and-forget consumer that handles FileLinkedToOwnerMessage notifications.")]
public sealed class FileLinkedToOwnerConsumer : AizenBaseMessageConsumer<FileLinkedToOwnerMessage>
{
    private readonly ILogger<FileLinkedToOwnerConsumer> _logger;

    public FileLinkedToOwnerConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<FileLinkedToOwnerConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(
        FileLinkedToOwnerMessage message, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public override Task ExecuteCommitMessage(
        FileLinkedToOwnerMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "File linked to owner: FileId={FileId}, Module={Module}, EntityType={EntityType}, EntityId={EntityId}",
            message.FileId, message.OwnerModule, message.OwnerEntityType, message.OwnerEntityId);
        return Task.CompletedTask;
    }

    public override Task ExecuteRollbackMessage(
        FileLinkedToOwnerMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
