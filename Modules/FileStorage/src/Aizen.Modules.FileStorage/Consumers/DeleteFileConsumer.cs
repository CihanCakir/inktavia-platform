using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Aizen.Modules.FileStorage.Application.Commands.DeleteFile;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.FileStorage.Consumers;

[DocumentationInfo("Delete file consumer", "Request/response consumer that soft-deletes a file via CQRS.")]
public sealed class DeleteFileConsumer
    : AizenBaseMessageConsumer<DeleteFileProcessMessage, DeleteFileProcessMessageResult>
{
    private readonly IAizenCQRSProcessor _cqrsProcessor;

    public DeleteFileConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _cqrsProcessor = serviceProvider.GetRequiredService<IAizenCQRSProcessor>();
    }

    public override Task<bool> ExecutePrepareMessage(
        DeleteFileProcessMessage message, CancellationToken cancellationToken)
        => Task.FromResult(message.FileId != Guid.Empty);

    public override async Task<DeleteFileProcessMessageResult> ExecuteCommitMessage(
        DeleteFileProcessMessage message, CancellationToken cancellationToken)
    {
        await _cqrsProcessor.ProcessAsync<bool>(
            new DeleteFileCommand
            {
                FileId = message.FileId,
                Request = new DeleteFileRequest { DeleteBehavior = message.DeleteBehavior }
            }, cancellationToken);

        return new DeleteFileProcessMessageResult { FileId = message.FileId, IsDeleted = true };
    }

    public override Task ExecuteRollbackMessage(
        DeleteFileProcessMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
