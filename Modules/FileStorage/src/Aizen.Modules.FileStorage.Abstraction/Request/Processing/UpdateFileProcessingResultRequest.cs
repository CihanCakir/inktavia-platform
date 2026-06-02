using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Request.Processing;

[DocumentationInfo("Update file processing result request", "Records the outcome of a completed background processing job.")]
public sealed class UpdateFileProcessingResultRequest
{
    public FileProcessingType ProcessingType { get; set; }
    public bool IsSuccess { get; set; }
    public string? ResultDocumentId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}
