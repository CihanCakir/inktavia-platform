using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Commands.DeleteFile;

[DocumentationInfo("Delete file command handler", "Loads the file entity, delegates to IFileStorageService to soft-delete, then publishes FileDeletedMessage.")]
public sealed class DeleteFileCommandHandler : AizenCommandHandler<DeleteFileCommand, bool>
{
    private readonly IFileStorageService _storageService;
    private readonly IFileRepository _fileRepository;
    private readonly IAizenMessagePublisher _publisher;
    private readonly IAizenInfoAccessor _info;

    public DeleteFileCommandHandler(
        IFileStorageService storageService,
        IFileRepository fileRepository,
        IAizenMessagePublisher publisher,
        IAizenInfoAccessor info)
    {
        _storageService = storageService;
        _fileRepository = fileRepository;
        _publisher = publisher;
        _info = info;
    }

    public override async Task<bool> Handle(DeleteFileCommand command, CancellationToken cancellationToken)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;

        var file = await _fileRepository.GetByIdAsync(command.FileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File with id '{command.FileId}' not found.");

        var fileId = file.PublicId ?? Guid.Empty;
        var objectKey = file.ObjectKey;
        var bucketName = file.BucketName;

        var deleted = await _storageService.DeleteFileAsync(command.FileId, userId, cancellationToken);

        if (deleted)
        {
            await _publisher.PublishAsync(new FileDeletedMessage
            {
                FileId = fileId,
                ObjectKey = objectKey,
                BucketName = bucketName,
                DeleteBehavior = command.Request?.DeleteBehavior ?? FileDeleteBehavior.SoftDeleteOnly,
                DeletedByUserId = userId
            }, cancellationToken);
        }

        return deleted;
    }
}
