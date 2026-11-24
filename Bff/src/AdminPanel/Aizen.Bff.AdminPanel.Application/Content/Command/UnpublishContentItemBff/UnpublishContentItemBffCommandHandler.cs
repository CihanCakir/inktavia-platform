using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

[DocumentationInfo("Unpublish content item command handler", "İçerik öğesini yayından kaldırır.")]
public sealed class UnpublishContentItemBffCommandHandler
    : AizenCommandHandler<UnpublishContentItemBffCommand, ContentItemBffDto>
{
    private readonly IContentRemoteCall _content;

    public UnpublishContentItemBffCommandHandler(IContentRemoteCall content) => _content = content;

    public override async Task<ContentItemBffDto?> Handle(
        UnpublishContentItemBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _content.Unpublish(request.Id);
        return ContentBffMapper.ToBff(result?.Body);
    }
}
