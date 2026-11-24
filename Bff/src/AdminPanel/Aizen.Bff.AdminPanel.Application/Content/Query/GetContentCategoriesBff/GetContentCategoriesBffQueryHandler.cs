using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Content.Query;

[DocumentationInfo("Get content categories query handler", "Content public kategori ağacını proxy eder (admin liste endpoint'i yok).")]
public sealed class GetContentCategoriesBffQueryHandler
    : AizenQueryHandler<GetContentCategoriesBffQuery, List<ContentCategoryBffDto>>
{
    private readonly IContentRemoteCall _content;

    public GetContentCategoriesBffQueryHandler(IContentRemoteCall content) => _content = content;

    public override async Task<List<ContentCategoryBffDto>> Handle(
        GetContentCategoriesBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _content.ListCategories(request.Lang);
        return (result ?? new List<Modules.Content.Abstraction.Dto.ContentCategoryDto>())
            .Select(ContentBffMapper.ToBff)
            .ToList();
    }
}
