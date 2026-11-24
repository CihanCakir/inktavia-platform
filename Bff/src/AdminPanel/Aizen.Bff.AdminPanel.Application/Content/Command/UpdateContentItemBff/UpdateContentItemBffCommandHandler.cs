using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

[DocumentationInfo("Update content item command handler", "İçerik öğesinin çekirdek editöryel alanlarını günceller.")]
public sealed class UpdateContentItemBffCommandHandler
    : AizenCommandHandler<UpdateContentItemBffCommand, ContentItemBffDto>
{
    private readonly IContentRemoteCall _content;

    public UpdateContentItemBffCommandHandler(IContentRemoteCall content) => _content = content;

    public override async Task<ContentItemBffDto?> Handle(
        UpdateContentItemBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _content.UpdateItem(request.Id, request.Body);
        return ContentBffMapper.ToBff(result?.Body);
    }
}
