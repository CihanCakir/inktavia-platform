using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;

namespace Aizen.Bff.AdminPanel.Application.Files.Query;

[DocumentationInfo("Get admin file list BFF query", "FileStorage modülünden sayfalanmış admin dosya listesini ister.")]
public sealed class GetAdminFileListBffQuery : AizenQuery<AdminFileListResult>
{
    public string? Search { get; }
    public string? ContentType { get; }
    public int Page { get; }
    public int PageSize { get; }

    public GetAdminFileListBffQuery(string? search, string? contentType, int page, int pageSize)
    {
        Search = search;
        ContentType = contentType;
        Page = page;
        PageSize = pageSize;
    }
}
