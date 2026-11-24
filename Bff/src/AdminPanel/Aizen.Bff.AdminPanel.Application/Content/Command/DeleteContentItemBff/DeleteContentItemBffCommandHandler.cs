using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

[DocumentationInfo("Delete content item command handler", "İçerik öğesini siler.")]
public sealed class DeleteContentItemBffCommandHandler
    : AizenCommandHandler<DeleteContentItemBffCommand, ContentItemBffDto>
{
    private readonly IContentRemoteCall _content;

    public DeleteContentItemBffCommandHandler(IContentRemoteCall content) => _content = content;

    public override async Task<ContentItemBffDto?> Handle(
        DeleteContentItemBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _content.Delete(request.Id);
        return ContentBffMapper.ToBff(result?.Body);
    }
}
