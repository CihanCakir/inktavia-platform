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

        if (file.Status != FileStatus.Ready)
            throw new InvalidOperationException("File is not ready.");

        var url = await _storageProvider.GenerateReadUrlAsync(file.BucketName, file.ObjectKey, expiresIn, cancellationToken);

        return new FileAccessUrlDto
        {
            FileId = file.PublicId ?? Guid.Empty,
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
