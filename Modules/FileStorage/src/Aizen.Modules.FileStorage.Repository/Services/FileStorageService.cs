using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Domain.Interface.Service;
using Aizen.Modules.FileStorage.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.FileStorage.Repository.Services;

[DocumentationInfo("File storage service", "Completes file upload by validating S3 object existence, verifying actual content-type and size via server-side inspection, and marking the file as uploaded.")]
public sealed class FileStorageService : IFileStorageService
{
    private const int MagicByteReadLength = 16;
    /// <summary>
    /// Maximum allowed deviation between declared and actual file size.
    /// 20% accounts for legitimate differences: JPEG compression variance, HTTP
    /// Content-Length vs stored size due to transfer encoding, and minor metadata overhead.
    /// Files exceeding this tolerance are rejected to prevent size-lie abuse (e.g., declaring
    /// a small size to bypass quota checks while uploading a much larger file).
    /// </summary>
    private const double SizeTolerance = 0.20;

    private static readonly byte[] PdfMagic = { 0x25, 0x50, 0x44, 0x46, 0x2D }; // %PDF-
    private static readonly byte[] JpegMagic = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    private readonly FileStorageDbContext _db;
    private readonly IFileRepository _fileRepository;
    private readonly IFileUploadSessionRepository _uploadSessionRepository;
    private readonly IObjectStorageProvider _storageProvider;
    private readonly IFileCacheInvalidationService _cacheInvalidation;
    private readonly IFileValidationService _validationService;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(
        FileStorageDbContext db,
        IFileRepository fileRepository,
        IFileUploadSessionRepository uploadSessionRepository,
        IObjectStorageProvider storageProvider,
        IFileCacheInvalidationService cacheInvalidation,
        IFileValidationService validationService,
        ILogger<FileStorageService> logger)
    {
        _db = db;
        _fileRepository = fileRepository;
        _uploadSessionRepository = uploadSessionRepository;
        _storageProvider = storageProvider;
        _cacheInvalidation = cacheInvalidation;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<FileDto> CompleteUploadAsync(string uploadSessionCode, string? checksum, CancellationToken cancellationToken = default)
    {
        var session = await _uploadSessionRepository.GetByCodeAsync(uploadSessionCode, cancellationToken)
            ?? throw new KeyNotFoundException($"Upload session '{uploadSessionCode}' not found.");

        if (session.IsExpired())
            throw new InvalidOperationException("Upload session has expired.");

        // --- Server-side verification: fetch actual metadata instead of just checking existence ---
        ObjectMetadataResult metadata;
        try
        {
            metadata = await _storageProvider.GetObjectMetadataAsync(session.BucketName, session.ObjectKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Object not found or inaccessible in storage for session '{SessionCode}'.", uploadSessionCode);
            throw new InvalidOperationException("File object not found in storage.");
        }

        var file = await _fileRepository.GetByIdAsync(session.FileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File with id '{session.FileId}' not found.");

        // --- Validate actual content-type against allowlist ---
        if (!_validationService.IsAllowedContentType(metadata.ContentType))
        {
            await RejectAndDeleteAsync(file, session.BucketName, session.ObjectKey, cancellationToken);
            throw new InvalidOperationException($"Actual content type '{metadata.ContentType}' is not allowed.");
        }

        // --- Validate actual size against category cap ---
        try
        {
            _validationService.ValidateFileSize(metadata.ContentLength, file.Category);
        }
        catch (InvalidOperationException)
        {
            await RejectAndDeleteAsync(file, session.BucketName, session.ObjectKey, cancellationToken);
            throw;
        }

        // --- Check size discrepancy between declared and actual ---
        // Reject files where the actual size deviates more than 20% from the declared size.
        // This prevents size-lie abuse (declaring a small file to bypass quota checks while
        // uploading a much larger one). The 20% tolerance accounts for legitimate differences
        // such as JPEG compression variance and HTTP Content-Length vs stored size differences.
        if (file.SizeInBytes > 0)
        {
            var declaredSize = (double)file.SizeInBytes;
            var actualSize = (double)metadata.ContentLength;
            var deviation = Math.Abs(actualSize - declaredSize) / declaredSize;
            if (deviation > SizeTolerance)
            {
                _logger.LogWarning(
                    "File '{FileCode}' declared size {DeclaredSize} but actual size is {ActualSize} (deviation {Deviation:P1}). Rejecting.",
                    file.FileCode, file.SizeInBytes, metadata.ContentLength, deviation);
                await RejectAndDeleteAsync(file, session.BucketName, session.ObjectKey, cancellationToken);
                throw new InvalidOperationException(
                    $"Actual file size ({metadata.ContentLength} bytes) deviates more than {SizeTolerance:P0} from declared size ({file.SizeInBytes} bytes). Upload rejected.");
            }
        }

        // --- Magic byte verification ---
        if (ShouldVerifyMagicBytes(metadata.ContentType))
        {
            var header = await _storageProvider.ReadFirstBytesAsync(session.BucketName, session.ObjectKey, MagicByteReadLength, cancellationToken);
            if (!MagicBytesMatch(metadata.ContentType, header))
            {
                await RejectAndDeleteAsync(file, session.BucketName, session.ObjectKey, cancellationToken);
                throw new InvalidOperationException(
                    $"File content does not match declared type '{metadata.ContentType}'. Magic byte verification failed.");
            }
        }

        // --- Persist actual size and mark uploaded ---
        file.UpdateActualSize(metadata.ContentLength);
        file.MarkUploaded(checksum);
        _fileRepository.Update(file);
        session.Complete();
        _uploadSessionRepository.Update(session);
        await _db.SaveChangesAsync(cancellationToken);

        await _cacheInvalidation.InvalidateFileAsync(file.Id, file.PublicId ?? Guid.Empty, file.FileCode, cancellationToken);
        await _cacheInvalidation.InvalidateUploadSessionAsync(uploadSessionCode, cancellationToken);

        return MapToDto(file);
    }

    private async Task RejectAndDeleteAsync(Domain.Entities.File.FileEntity file, string bucketName, string objectKey, CancellationToken cancellationToken)
    {
        file.MarkRejected();
        _fileRepository.Update(file);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            await _storageProvider.DeleteObjectAsync(bucketName, objectKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete rejected object '{ObjectKey}' from bucket '{BucketName}'.", objectKey, bucketName);
        }

        await _cacheInvalidation.InvalidateFileAsync(file.Id, file.PublicId ?? Guid.Empty, file.FileCode, cancellationToken);
    }

    private static bool ShouldVerifyMagicBytes(string contentType) =>
        contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) ||
        contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) ||
        contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase);

    private static bool MagicBytesMatch(string contentType, byte[] header)
    {
        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            return StartsWith(header, PdfMagic);

        if (contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
            return StartsWith(header, JpegMagic);

        if (contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
            return StartsWith(header, PngMagic);

        return true; // unknown type — skip magic check
    }

    private static bool StartsWith(byte[] data, byte[] prefix) =>
        data.Length >= prefix.Length && data.AsSpan(0, prefix.Length).SequenceEqual(prefix);

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

        // Release the claims first. Soft-deleting the row alone leaves the owner references ACTIVE, and the
        // orphan-cleanup sweep skips both claimed files and soft-deleted ones — so the object would stay in the
        // bucket forever. That is the leak this phase exists to close: the delete has to unlink, not just hide.
        var ownerReferences = await _db.FileOwnerReferences
            .Where(r => r.FileId == fileId && r.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var reference in ownerReferences)
            reference.Deactivate();

        // Nothing references the file any more, so the bytes go now. The row survives as the audit record.
        try
        {
            await _storageProvider.DeleteObjectAsync(file.BucketName, file.ObjectKey, cancellationToken);
        }
        catch (Exception ex)
        {
            // Leave the file unclaimed and NOT soft-deleted, so the orphan-cleanup sweep retries the object
            // deletion on its next pass. Failing to purge the bytes must not be recorded as a successful delete.
            _logger.LogError(ex, "Failed to delete object '{ObjectKey}' from bucket '{BucketName}'; the orphan cleanup sweep will retry.",
                file.ObjectKey, file.BucketName);
            await _db.SaveChangesAsync(cancellationToken);
            await _cacheInvalidation.InvalidateFileAsync(file.Id, file.PublicId ?? Guid.Empty, file.FileCode, cancellationToken);
            return false;
        }

        file.SoftDelete(deletedByUserId);
        _fileRepository.Update(file);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("File {FileId} deleted: {ReferenceCount} owner reference(s) released, object purged from storage.",
            fileId, ownerReferences.Count);

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
