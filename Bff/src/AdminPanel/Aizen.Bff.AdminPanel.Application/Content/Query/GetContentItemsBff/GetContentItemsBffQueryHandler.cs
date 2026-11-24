using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Content.Query;

[DocumentationInfo("Get content items paged query handler",
    "Content modülünün tüm-statülü admin içerik listesini (type/status/surface/tag/lang) proxy eder.")]
public sealed class GetContentItemsBffQueryHandler
    : AizenQueryHandler<GetContentItemsBffQuery, ContentFeedBffResult>
{
    private readonly IContentRemoteCall _content;

    public GetContentItemsBffQueryHandler(IContentRemoteCall content) => _content = content;

    public override async Task<ContentFeedBffResult> Handle(
        GetContentItemsBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _content.ListItems(
            request.Type, request.Status, request.Surface, request.Tag,
            request.Lang, request.Page, request.PageSize);

        return ContentBffMapper.ToBff(result?.Body);
    }
}
