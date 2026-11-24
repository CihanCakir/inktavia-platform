using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

[DocumentationInfo("Publish content item command handler", "İçerik öğesini şimdi yayınlar.")]
public sealed class PublishContentItemBffCommandHandler
    : AizenCommandHandler<PublishContentItemBffCommand, ContentItemBffDto>
{
    private readonly IContentRemoteCall _content;

    public PublishContentItemBffCommandHandler(IContentRemoteCall content) => _content = content;

    public override async Task<ContentItemBffDto?> Handle(
        PublishContentItemBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _content.Publish(request.Id);
        return ContentBffMapper.ToBff(result?.Body);
    }
}
