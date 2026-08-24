using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Application.Mapping;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;

namespace Aizen.Modules.FileStorage.Application.Queries.GetAdminFileList;

[DocumentationInfo("Get admin file list query handler", "Repository'den sayfalanmış dosyaları alıp AdminFileListItemDto'ya projekte eder.")]
public sealed class GetAdminFileListQueryHandler : AizenQueryHandler<GetAdminFileListQuery, AdminFileListResult>
{
    private readonly IFileRepository _fileRepository;

    public GetAdminFileListQueryHandler(IFileRepository fileRepository)
    {
        _fileRepository = fileRepository;
    }

    public override async Task<AdminFileListResult> Handle(GetAdminFileListQuery request, CancellationToken cancellationToken)
    {
        // Sayfa/boyut değerlerini güvenli aralığa çek (1 tabanlı sayfa, makul üst sınır).
        var page = request.Request.Page < 1 ? 1 : request.Request.Page;
        var pageSize = request.Request.PageSize is < 1 or > 200 ? 20 : request.Request.PageSize;
        var skip = (page - 1) * pageSize;

        var (items, total) = await _fileRepository.GetAdminPagedAsync(
            request.Request.Search,
            request.Request.ContentType,
            skip,
            pageSize,
            cancellationToken);

        return new AdminFileListResult
        {
            Items = items.Select(f => f.ToAdminFileListItemDto()).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
