using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Dto.Processing;

[DocumentationInfo("File processing result DTO", "Carries the result data from a completed processing job.")]
public sealed class FileProcessingResultDto
{
    public Guid FileId { get; set; }
    public FileProcessingType ProcessingType { get; set; }
    public bool IsSuccess { get; set; }
    public string? ResultDocumentId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CompletedAt { get; set; }
}
