using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.FileStorage.Consumers;

[DocumentationInfo("File thumbnail requested consumer", "Fire-and-forget consumer that handles thumbnail generation requests for image files.")]
public sealed class FileThumbnailRequestedConsumer : AizenBaseMessageConsumer<FileThumbnailRequestedMessage>
{
    private readonly ILogger<FileThumbnailRequestedConsumer> _logger;

    public FileThumbnailRequestedConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<FileThumbnailRequestedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(
        FileThumbnailRequestedMessage message, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public override Task ExecuteCommitMessage(
        FileThumbnailRequestedMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Thumbnail generation requested: FileId={FileId}, ContentType={ContentType}, ObjectKey={ObjectKey}",
            message.FileId, message.ContentType, message.ObjectKey);
        return Task.CompletedTask;
    }

    public override Task ExecuteRollbackMessage(
        FileThumbnailRequestedMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
