using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.FileStorage.Consumers;

[DocumentationInfo("File deleted consumer", "Fire-and-forget consumer that handles FileDeletedMessage notifications.")]
public sealed class FileDeletedConsumer : AizenBaseMessageConsumer<FileDeletedMessage>
{
    private readonly ILogger<FileDeletedConsumer> _logger;

    public FileDeletedConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<FileDeletedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(
        FileDeletedMessage message, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public override Task ExecuteCommitMessage(
        FileDeletedMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "File deleted: FileId={FileId}, Behavior={Behavior}, ObjectKey={ObjectKey}, DeletedBy={UserId}",
            message.FileId, message.DeleteBehavior, message.ObjectKey, message.DeletedByUserId);
        return Task.CompletedTask;
    }

    public override Task ExecuteRollbackMessage(
        FileDeletedMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
