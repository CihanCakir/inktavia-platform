using Aizen.Core.CQRS.Handler;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Commands.CompleteUploadSession;

[DocumentationInfo("Complete upload session command handler", "Delegates to IFileStorageService to complete the upload, then publishes FileUploadedMessage.")]
public sealed class CompleteUploadSessionCommandHandler : AizenCommandHandler<CompleteUploadSessionCommand, FileDto>
{
    private readonly IFileStorageService _storageService;
    private readonly IAizenMessagePublisher _publisher;

    public CompleteUploadSessionCommandHandler(IFileStorageService storageService, IAizenMessagePublisher publisher)
    {
        _storageService = storageService;
        _publisher = publisher;
    }

    public override async Task<FileDto?> Handle(CompleteUploadSessionCommand command, CancellationToken cancellationToken)
    {
        var fileDto = await _storageService.CompleteUploadAsync(
            command.UploadSessionCode, command.Request?.Checksum, cancellationToken);

        await _publisher.PublishAsync(new FileUploadedMessage
        {
            FileId = fileDto.FileId,
            FileCode = fileDto.FileCode,
            ObjectKey = fileDto.ObjectKey,
            BucketName = fileDto.BucketName,
            ContentType = fileDto.ContentType,
            SizeInBytes = fileDto.SizeInBytes,
            UploadedByUserId = fileDto.UploadedByUserId
        }, cancellationToken);

        return fileDto;
    }
}
