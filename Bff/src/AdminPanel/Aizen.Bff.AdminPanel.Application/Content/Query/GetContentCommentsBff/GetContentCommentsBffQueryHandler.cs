using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Content.Query;

[DocumentationInfo("Get content comments query handler", "Bir içerik öğesinin moderasyon yorum kuyruğunu proxy eder.")]
public sealed class GetContentCommentsBffQueryHandler
    : AizenQueryHandler<GetContentCommentsBffQuery, ContentCommentsBffResult>
{
    private readonly IContentRemoteCall _content;

    public GetContentCommentsBffQueryHandler(IContentRemoteCall content) => _content = content;

    public override async Task<ContentCommentsBffResult> Handle(
        GetContentCommentsBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _content.ListComments(
            request.ContentId, request.Status, request.Page, request.PageSize);

        return ContentBffMapper.ToBff(result?.Body);
    }
}
