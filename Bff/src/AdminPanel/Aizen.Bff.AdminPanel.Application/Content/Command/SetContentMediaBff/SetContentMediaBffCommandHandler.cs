using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

[DocumentationInfo("Set content media command handler", "İçerik öğesinin medya koleksiyonunu (kapak + galeri) değiştirir.")]
public sealed class SetContentMediaBffCommandHandler
    : AizenCommandHandler<SetContentMediaBffCommand, ContentItemBffDto>
{
    private readonly IContentRemoteCall _content;

    public SetContentMediaBffCommandHandler(IContentRemoteCall content) => _content = content;

    public override async Task<ContentItemBffDto?> Handle(
        SetContentMediaBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _content.SetMedia(request.Id, request.Body);
        return ContentBffMapper.ToBff(result?.Body);
    }
}
