using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Repository.Services;

[DocumentationInfo("File access service", "Validates file access rights and generates pre-signed S3 read URLs.")]
public sealed class FileAccessService : IFileAccessService
{
    private readonly IFileRepository _fileRepository;
    private readonly IObjectStorageProvider _storageProvider;

    public FileAccessService(IFileRepository fileRepository, IObjectStorageProvider storageProvider)
    {
        _fileRepository = fileRepository;
        _storageProvider = storageProvider;
    }

    public async Task<FileAccessUrlDto> CreateReadUrlAsync(long fileId, TimeSpan expiresIn, long? requestedByUserId, CancellationToken cancellationToken = default)
    {
        var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File with id '{fileId}' not found.");

        // A file is readable once its bytes are in the bucket. `Ready` is only reached by the post-upload
        // processing/scan pipeline, which does not exist yet — requiring it here made every read URL throw for
        // freshly uploaded files, while attach already accepts Uploaded|Ready. Allow-list the readable states
        // and reject everything else by name, so Quarantined/Rejected/Deleted stay blocked (fail closed).
        if (file.Status is not (FileStatus.Uploaded or FileStatus.Ready))
            throw new InvalidOperationException($"File is not readable (status: {file.Status}).");

        var url = await _storageProvider.GenerateReadUrlAsync(file.BucketName, file.ObjectKey, expiresIn, cancellationToken);

        return new FileAccessUrlDto
        {
            FileId = file.PublicId ?? throw new InvalidOperationException($"File {file.Id} has no PublicId — this is a data integrity bug."),
            ReadUrl = url,
            ExpiresAt = DateTime.UtcNow.Add(expiresIn)
        };
    }

    public async Task EnsureCanReadAsync(long fileId, long? requestedByUserId, CancellationToken cancellationToken = default)
    {
        var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File with id '{fileId}' not found.");

        if (file.Status == FileStatus.Deleted)
            throw new InvalidOperationException("File has been deleted.");

        if (file.Visibility == FileVisibility.Private && requestedByUserId != file.UploadedByUserId)
            throw new UnauthorizedAccessException("Access to this file is restricted.");
    }
}
