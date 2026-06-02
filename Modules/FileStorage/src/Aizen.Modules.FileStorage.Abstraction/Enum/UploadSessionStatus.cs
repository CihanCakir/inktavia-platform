using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Enum;

[DocumentationInfo("Upload session status", "Tracks the lifecycle of an S3 pre-signed upload session.")]
public enum UploadSessionStatus { Active = 1, Completed = 2, Expired = 3, Cancelled = 4 }
