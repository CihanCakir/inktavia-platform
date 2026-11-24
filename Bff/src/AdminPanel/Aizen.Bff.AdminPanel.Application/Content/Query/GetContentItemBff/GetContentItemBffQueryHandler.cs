using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Content.Query;

[DocumentationInfo("Get content item detail query handler", "Content admin get-by-id detayını proxy eder.")]
public sealed class GetContentItemBffQueryHandler
    : AizenQueryHandler<GetContentItemBffQuery, ContentItemBffDto>
{
    private readonly IContentRemoteCall _content;

    public GetContentItemBffQueryHandler(IContentRemoteCall content) => _content = content;

    public override async Task<ContentItemBffDto?> Handle(
        GetContentItemBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _content.GetItem(request.Id);
        return ContentBffMapper.ToBff(result?.Body);
    }
}
