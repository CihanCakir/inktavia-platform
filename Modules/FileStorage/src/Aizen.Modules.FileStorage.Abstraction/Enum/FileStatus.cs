
namespace Aizen.Modules.FileStorage.Abstraction.Enum;

[DocumentationInfo("File lifecycle status", "Tracks the lifecycle state of a stored file.")]
public enum FileStatus { Created = 1, UploadUrlGenerated = 2, Uploaded = 3, Processing = 4, Ready = 5, Rejected = 6, Deleted = 7, Orphaned = 8, Quarantined = 9 }
