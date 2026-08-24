using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;

namespace Aizen.Bff.AdminPanel.Application.Files.Query;

[DocumentationInfo("Get admin file list BFF query handler", "FileStorage modülünün admin dosya listesi uç noktasını proxy eder.")]
public sealed class GetAdminFileListBffQueryHandler
    : AizenQueryHandler<GetAdminFileListBffQuery, AdminFileListResult>
{
    private readonly IFileStorageRemoteCall _fileStorage;

    public GetAdminFileListBffQueryHandler(IFileStorageRemoteCall fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public override async Task<AdminFileListResult> Handle(GetAdminFileListBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _fileStorage.GetAdminFiles(request.Search, request.ContentType, request.Page, request.PageSize);

        // Modül boş tabloda dahi dolu bir zarf döner; yine de gövde null gelirse
        // sözleşmeyi koruyacak boş bir sayfa döneriz (hata değil).
        return result?.Body ?? new AdminFileListResult
        {
            Items = new List<AdminFileListItemDto>(),
            TotalCount = 0,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
