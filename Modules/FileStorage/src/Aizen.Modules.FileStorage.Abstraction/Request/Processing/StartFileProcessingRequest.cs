using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Request.Processing;

[DocumentationInfo("Start file processing request", "Initiates a background processing job for a file.")]
public sealed class StartFileProcessingRequest
{
    public FileProcessingType ProcessingType { get; set; }
}
