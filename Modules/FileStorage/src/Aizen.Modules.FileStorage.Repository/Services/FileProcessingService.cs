using Aizen.Modules.FileStorage.Abstraction.Dto.Processing;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Abstraction.Request.Processing;
using Aizen.Modules.FileStorage.Domain.Entities.Processing;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Domain.Interface.Service;
using Aizen.Modules.FileStorage.Repository.Persistence;

namespace Aizen.Modules.FileStorage.Repository.Services;

[DocumentationInfo("File processing service", "Creates and manages background processing jobs for file post-processing.")]
public sealed class FileProcessingService : IFileProcessingService
{
    private readonly FileStorageDbContext _db;
    private readonly IFileRepository _fileRepository;
    private readonly IFileProcessingJobRepository _jobRepository;
    private readonly IFileCacheInvalidationService _cacheInvalidation;

    public FileProcessingService(
        FileStorageDbContext db,
        IFileRepository fileRepository,
        IFileProcessingJobRepository jobRepository,
        IFileCacheInvalidationService cacheInvalidation)
    {
        _db = db;
        _fileRepository = fileRepository;
        _jobRepository = jobRepository;
        _cacheInvalidation = cacheInvalidation;
    }

    public async Task<FileProcessingJobDto> StartProcessingAsync(long fileId, FileProcessingType processingType, CancellationToken cancellationToken = default)
    {
        var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File with id '{fileId}' not found.");

        var job = FileProcessingJobEntity.Create(fileId, processingType);
        await _jobRepository.AddAsync(job, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        await _cacheInvalidation.InvalidateProcessingJobsAsync(fileId, cancellationToken);

        return MapToDto(job, file.PublicId ?? Guid.Empty);
    }

    public async Task<bool> UpdateResultAsync(long fileId, UpdateFileProcessingResultRequest request, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByFileAndTypeAsync(fileId, request.ProcessingType, cancellationToken);
        if (job is null) return false;

        if (request.IsSuccess)
            job.Complete(request.ResultDocumentId);
        else
            job.Fail(request.ErrorCode ?? "UNKNOWN_ERROR", request.ErrorMessage ?? "Processing failed.");

        _jobRepository.Update(job);
        await _db.SaveChangesAsync(cancellationToken);

        await _cacheInvalidation.InvalidateProcessingJobsAsync(fileId, cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<FileProcessingJobDto>> GetJobsAsync(long fileId, CancellationToken cancellationToken = default)
    {
        var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken);
        var fileGuid = file?.PublicId ?? Guid.Empty;

        var jobs = await _jobRepository.GetByFileIdAsync(fileId, cancellationToken);
        return jobs.Select(j => MapToDto(j, fileGuid)).ToList();
    }

    private static FileProcessingJobDto MapToDto(FileProcessingJobEntity job, Guid fileGuid) => new()
    {
        FileId = fileGuid,
        ProcessingType = job.ProcessingType,
        Status = job.Status,
        RequestedAt = job.RequestedAt,
        StartedAt = job.StartedAt,
        CompletedAt = job.CompletedAt,
        ErrorCode = job.ErrorCode,
        ErrorMessage = job.ErrorMessage,
        RetryCount = job.RetryCount
    };
}
