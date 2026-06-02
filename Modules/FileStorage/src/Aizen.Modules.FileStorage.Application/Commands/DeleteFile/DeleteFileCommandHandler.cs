using Aizen.Core.CQRS.Handler;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Commands.DeleteFile;

[DocumentationInfo("Delete file command handler", "Loads the file entity, delegates to IFileStorageService to soft-delete, then publishes FileDeletedMessage.")]
public sealed class DeleteFileCommandHandler : AizenCommandHandler<DeleteFileCommand, bool>
{
    private readonly IFileStorageService _storageService;
    private readonly IFileRepository _fileRepository;
    private readonly IAizenMessagePublisher _publisher;

    public DeleteFileCommandHandler(
        IFileStorageService storageService,
        IFileRepository fileRepository,
        IAizenMessagePublisher publisher)
    {
        _storageService = storageService;
        _fileRepository = fileRepository;
        _publisher = publisher;
    }

    public override async Task<bool> Handle(DeleteFileCommand command, CancellationToken cancellationToken)
    {
        var file = await _fileRepository.GetByIdAsync(command.FileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File with id '{command.FileId}' not found.");

        var fileId = file.PublicId ?? Guid.Empty;
        var objectKey = file.ObjectKey;
        var bucketName = file.BucketName;

        var deleted = await _storageService.DeleteFileAsync(command.FileId, command.UserId, cancellationToken);

        if (deleted)
        {
            await _publisher.PublishAsync(new FileDeletedMessage
            {
                FileId = fileId,
                ObjectKey = objectKey,
                BucketName = bucketName,
                DeleteBehavior = command.Request?.DeleteBehavior ?? FileDeleteBehavior.SoftDeleteOnly,
                DeletedByUserId = command.UserId
            }, cancellationToken);
        }

        return deleted;
    }
}
