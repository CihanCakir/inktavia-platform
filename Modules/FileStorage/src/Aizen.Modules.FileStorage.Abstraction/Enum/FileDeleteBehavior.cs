
namespace Aizen.Modules.FileStorage.Abstraction.Enum;

[DocumentationInfo("File delete behavior", "Controls whether S3 object is physically deleted or only soft-deleted in the database.")]
public enum FileDeleteBehavior { SoftDeleteOnly = 1, SoftDeleteAndRemoveObject = 2 }
