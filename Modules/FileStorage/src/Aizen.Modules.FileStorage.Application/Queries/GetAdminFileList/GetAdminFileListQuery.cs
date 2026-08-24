using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Request.File;

namespace Aizen.Modules.FileStorage.Application.Queries.GetAdminFileList;

[DocumentationInfo("Get admin file list query", "Admin için sayfalanmış, en yeni önce, ada göre aranabilir dosya listesi döner.")]
public sealed class GetAdminFileListQuery : AizenQuery<AdminFileListResult>
{
    public AdminFileListRequest Request { get; }

    public GetAdminFileListQuery(AdminFileListRequest request)
    {
        Request = request;
    }
}
