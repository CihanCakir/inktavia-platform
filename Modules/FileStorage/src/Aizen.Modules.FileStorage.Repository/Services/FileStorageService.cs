using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Domain.Interface.Service;
using Aizen.Modules.FileStorage.Repository.Persistence;

namespace Aizen.Modules.FileStorage.Repository.Services;

[DocumentationInfo("File storage service", "Completes file upload by validating S3 object existence and marking the file as uploaded.")]
public sealed class FileStorageService : IFileStorageService
{
    private readonly FileStorageDbContext _db;
    private readonly IFileRepository _fileRepository;
    private readonly IFileUploadSessionRepository _uploadSessionRepository;
    private readonly IObjectStorageProvider _storageProvider;
    private readonly IFileCacheInvalidationService _cacheInvalidation;

    public FileStorageService(
        FileStorageDbContext db,
        IFileRepository fileRepository,
        IFileUploadSessionRepository uploadSessionRepository,
        IObjectStorageProvider storageProvider,
        IFileCacheInvalidationService cacheInvalidation)
    {
        _db = db;
        _fileRepository = fileRepository;
        _uploadSessionRepository = uploadSessionRepository;
        _storageProvider = storageProvider;
        _cacheInvalidation = cacheInvalidation;
    }

    public async Task<FileDto> CompleteUploadAsync(string uploadSessionCode, string? checksum, CancellationToken cancellationToken = default)
    {
        var session = await _uploadSessionRepository.GetByCodeAsync(uploadSessionCode, cancellationToken)
            ?? throw new KeyNotFoundException($"Upload session '{uploadSessionCode}' not found.");

        if (session.IsExpired())
            throw new InvalidOperationException("Upload session has expired.");

        var exists = await _storageProvider.ObjectExistsAsync(session.BucketName, session.ObjectKey, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("File object not found in storage.");

        var file = await _fileRepository.GetByIdAsync(session.FileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File with id '{session.FileId}' not found.");

        file.MarkUploaded(checksum);
        _fileRepository.Update(file);
        session.Complete();
        _uploadSessionRepository.Update(session);
        await _db.SaveChangesAsync(cancellationToken);

        await _cacheInvalidation.InvalidateFileAsync(file.Id, file.PublicId ?? Guid.Empty, file.FileCode, cancellationToken);
        await _cacheInvalidation.InvalidateUploadSessionAsync(uploadSessionCode, cancellationToken);

        return MapToDto(file);
    }

    private static FileDto MapToDto(Domain.Entities.File.FileEntity file) => new()
    {
        FileId = file.PublicId ?? Guid.Empty,
        FileCode = file.FileCode,
        OriginalFileName = file.OriginalFileName,
        StoredFileName = file.StoredFileName,
        BucketName = file.BucketName,
        ObjectKey = file.ObjectKey,
        ContentType = file.ContentType,
        Extension = file.Extension,
        SizeInBytes = file.SizeInBytes,
        Checksum = file.Checksum,
        StorageProvider = file.StorageProvider,
        Visibility = file.Visibility,
        Category = file.Category,
        Status = file.Status,
        UploadedAt = file.UploadedAt,
        UploadedByUserId = file.UploadedByUserId,
        CreatedAt = file.CreateDate ?? DateTime.UtcNow
    };

    public async Task<bool> DeleteFileAsync(long fileId, long? deletedByUserId, CancellationToken cancellationToken = default)
    {
        var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File with id '{fileId}' not found.");

        file.SoftDelete(deletedByUserId);
        _fileRepository.Update(file);
        await _db.SaveChangesAsync(cancellationToken);

        await _cacheInvalidation.InvalidateFileAsync(file.Id, file.PublicId ?? Guid.Empty, file.FileCode, cancellationToken);

        return true;
    }

    public async Task<FileDto> UpdateVisibilityAsync(long fileId, FileVisibility visibility, CancellationToken cancellationToken = default)
    {
        var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File with id '{fileId}' not found.");

        file.UpdateVisibility(visibility);
        _fileRepository.Update(file);
        await _db.SaveChangesAsync(cancellationToken);

        await _cacheInvalidation.InvalidateFileAsync(file.Id, file.PublicId ?? Guid.Empty, file.FileCode, cancellationToken);

        return MapToDto(file);
    }

    public async Task<bool> RejectFileAsync(long fileId, CancellationToken cancellationToken = default)
    {
        var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File with id '{fileId}' not found.");

        file.Reject();
        _fileRepository.Update(file);
        await _db.SaveChangesAsync(cancellationToken);

        await _cacheInvalidation.InvalidateFileAsync(file.Id, file.PublicId ?? Guid.Empty, file.FileCode, cancellationToken);

        return true;
    }
}
