
namespace Aizen.Modules.FileStorage.Abstraction.Enum;

[DocumentationInfo("File processing status", "Tracks the state of a background processing job.")]
public enum FileProcessingStatus { Pending = 1, InProgress = 2, Completed = 3, Failed = 4, Skipped = 5 }
