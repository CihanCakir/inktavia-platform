using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Aizen.Modules.FileStorage.Application.Commands.DeleteFile;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.FileStorage.Consumers;

[DocumentationInfo("Delete file consumer", "Request/response consumer that soft-deletes a file via CQRS.")]
public sealed class DeleteFileConsumer
    : AizenBaseMessageConsumer<DeleteFileProcessMessage, DeleteFileProcessMessageResult>
{
    private readonly IAizenCQRSProcessor _cqrsProcessor;
    private readonly IFileRepository _fileRepository;

    public DeleteFileConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _cqrsProcessor = serviceProvider.GetRequiredService<IAizenCQRSProcessor>();
        _fileRepository = serviceProvider.GetRequiredService<IFileRepository>();
    }

    public override Task<bool> ExecutePrepareMessage(
        DeleteFileProcessMessage message, CancellationToken cancellationToken)
        => Task.FromResult(message.FileId != Guid.Empty);

    public override async Task<DeleteFileProcessMessageResult> ExecuteCommitMessage(
        DeleteFileProcessMessage message, CancellationToken cancellationToken)
    {
        var fileEntity = await _fileRepository.GetByGuidAsync(message.FileId, cancellationToken);
        if (fileEntity is null)
        {
            return new DeleteFileProcessMessageResult { FileId = message.FileId, IsDeleted = false };
        }

        await _cqrsProcessor.ProcessAsync<bool>(
            new DeleteFileCommand
            {
                FileId = fileEntity.Id,
                Request = new DeleteFileRequest { DeleteBehavior = message.DeleteBehavior },
                UserId = message.DeletedByUserId
            }, cancellationToken);

        return new DeleteFileProcessMessageResult { FileId = message.FileId, IsDeleted = true };
    }

    public override Task ExecuteRollbackMessage(
        DeleteFileProcessMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
