using Aizen.Modules.FileStorage.Abstraction.Dto.UploadSession;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;
using Aizen.Modules.FileStorage.Domain.Entities.File;
using Aizen.Modules.FileStorage.Domain.Entities.UploadSession;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Domain.Interface.Service;
using Aizen.Modules.FileStorage.Repository.Persistence;
using Aizen.Modules.FileStorage.Repository.Providers.S3;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.FileStorage.Repository.Services;

[DocumentationInfo("File upload session service", "Creates FileEntity, FileUploadSessionEntity, generates S3 object key and pre-signed upload URL.")]
public sealed class FileUploadSessionService : IFileUploadSessionService
{
    private readonly FileStorageDbContext _db;
    private readonly IFileRepository _fileRepository;
    private readonly IFileUploadSessionRepository _uploadSessionRepository;
    private readonly IObjectStorageProvider _storageProvider;
    private readonly IFileValidationService _validationService;
    private readonly S3ObjectStorageOptions _options;

    public FileUploadSessionService(
        FileStorageDbContext db,
        IFileRepository fileRepository,
        IFileUploadSessionRepository uploadSessionRepository,
        IObjectStorageProvider storageProvider,
        IFileValidationService validationService,
        IOptions<S3ObjectStorageOptions> options)
    {
        _db = db;
        _fileRepository = fileRepository;
        _uploadSessionRepository = uploadSessionRepository;
        _storageProvider = storageProvider;
        _validationService = validationService;
        _options = options.Value;
    }

    public async Task<FileUploadSessionDto> CreateUploadSessionAsync(
        CreateUploadSessionRequest request,
        long? userId,
        string? clientId,
        string? deviceId,
        CancellationToken cancellationToken = default)
    {
        _validationService.ValidateContentType(request.ContentType);
        var extension = Path.GetExtension(request.OriginalFileName).TrimStart('.');
        _validationService.ValidateExtension(extension);
        _validationService.ValidateFileSize(request.SizeInBytes, request.Category);

        var fileCode = Guid.NewGuid().ToString("N").ToUpperInvariant();
        var storedFileName = $"{fileCode}.{extension}";
        var bucketName = _options.BucketName;
        var objectKey = $"{request.Category.ToString().ToLower()}/{DateTime.UtcNow:yyyy/MM}/{storedFileName}";

        var file = FileEntity.Create(
            fileCode,
            request.OriginalFileName,
            storedFileName,
            bucketName,
            objectKey,
            request.ContentType,
            extension,
            request.SizeInBytes,
            StorageProviderType.AwsS3,
            request.Visibility,
            request.Category,
            userId);

        file.MarkUploadUrlGenerated();
        await _fileRepository.AddAsync(file, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var expiresIn = TimeSpan.FromMinutes(_options.UploadUrlExpirationMinutes);
        var uploadUrl = await _storageProvider.GenerateUploadUrlAsync(bucketName, objectKey, request.ContentType, expiresIn, cancellationToken);

        var sessionCode = Guid.NewGuid().ToString("N").ToUpperInvariant();
        var session = FileUploadSessionEntity.Create(
            file.Id,
            sessionCode,
            bucketName,
            objectKey,
            DateTime.UtcNow.Add(expiresIn),
            request.OriginalFileName,
            request.ContentType,
            request.SizeInBytes,
            userId,
            clientId,
            deviceId);

        await _uploadSessionRepository.AddAsync(session, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new FileUploadSessionDto
        {
            FileId = file.PublicId ?? throw new InvalidOperationException("FileEntity.PublicId was not assigned."),
            UploadSessionCode = sessionCode,
            UploadUrl = uploadUrl,
            BucketName = bucketName,
            ObjectKey = objectKey,
            ExpiresAt = session.ExpiresAt,
            Status = session.Status,
            RequestedFileName = request.OriginalFileName,
            RequestedContentType = request.ContentType,
            RequestedSizeInBytes = request.SizeInBytes
        };
    }
}
