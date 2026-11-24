using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

[DocumentationInfo("Archive content item command handler", "İçerik öğesini arşivler.")]
public sealed class ArchiveContentItemBffCommandHandler
    : AizenCommandHandler<ArchiveContentItemBffCommand, ContentItemBffDto>
{
    private readonly IContentRemoteCall _content;

    public ArchiveContentItemBffCommandHandler(IContentRemoteCall content) => _content = content;

    public override async Task<ContentItemBffDto?> Handle(
        ArchiveContentItemBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _content.Archive(request.Id);
        return ContentBffMapper.ToBff(result?.Body);
    }
}
