namespace Aizen.Modules.FileStorage.Abstraction.Dto.File;

[DocumentationInfo("Admin file list result", "Admin dosya listeleme sonucunun sayfalama zarfı (FE bu sözleşmeye göre yazılacak).")]
public sealed class AdminFileListResult
{
    public IReadOnlyList<AdminFileListItemDto> Items { get; set; } = new List<AdminFileListItemDto>();

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}
