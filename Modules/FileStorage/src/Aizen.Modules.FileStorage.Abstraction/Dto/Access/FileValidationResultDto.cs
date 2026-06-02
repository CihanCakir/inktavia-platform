using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Dto.Access;

[DocumentationInfo("File validation result DTO", "Result of a file ownership or access validation check.")]
public sealed class FileValidationResultDto
{
    public Guid FileId { get; set; }
    public bool IsValid { get; set; }
    public string? Reason { get; set; }
}
