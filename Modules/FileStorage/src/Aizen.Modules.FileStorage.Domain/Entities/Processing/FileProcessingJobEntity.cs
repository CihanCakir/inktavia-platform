using Aizen.Core.Domain;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Domain.Entities.File;

namespace Aizen.Modules.FileStorage.Domain.Entities.Processing;

[DocumentationInfo("File processing job entity", "Tracks a background processing job (virus scan, thumbnail, metadata extraction) for a file.")]
public sealed class FileProcessingJobEntity : AizenEntityWithAudit
{
    public long FileId { get; private set; }
    public FileProcessingType ProcessingType { get; private set; }
    public FileProcessingStatus Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ResultDocumentId { get; private set; }
    public int RetryCount { get; private set; }

    public FileEntity? File { get; private set; }

    public FileProcessingJobEntity() { }

    public static FileProcessingJobEntity Create(long fileId, FileProcessingType processingType)
    {
        return new FileProcessingJobEntity
        {
            FileId = fileId,
            ProcessingType = processingType,
            Status = FileProcessingStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void Start()
    {
        Status = FileProcessingStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete(string? resultDocumentId = null)
    {
        Status = FileProcessingStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        ResultDocumentId = resultDocumentId;
    }

    public void Fail(string errorCode, string errorMessage)
    {
        Status = FileProcessingStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        RetryCount++;
    }
}
