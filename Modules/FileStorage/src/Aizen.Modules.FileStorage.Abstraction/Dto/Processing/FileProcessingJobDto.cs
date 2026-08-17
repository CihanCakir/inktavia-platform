using Aizen.Modules.FileStorage.Abstraction.Enum;

namespace Aizen.Modules.FileStorage.Abstraction.Dto.Processing;

[DocumentationInfo("File processing job DTO", "Represents a background processing job applied to a file.")]
public sealed class FileProcessingJobDto
{
    public Guid FileId { get; set; }
    public FileProcessingType ProcessingType { get; set; }
    public FileProcessingStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}
