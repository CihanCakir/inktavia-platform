using Aizen.Modules.FileStorage.Abstraction.Enum;

namespace Aizen.Modules.FileStorage.Abstraction.Request.File;

[DocumentationInfo("Filter files request", "Paginated query for files with optional status, category and owner filters.")]
public sealed class FilterFilesRequest
{
    public FileStatus? Status { get; set; }
    public FileCategory? Category { get; set; }
    public FileVisibility? Visibility { get; set; }
    public string? OwnerModule { get; set; }
    public string? OwnerEntityType { get; set; }
    public Guid? OwnerEntityId { get; set; }
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
}
