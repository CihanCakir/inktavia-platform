using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

[DocumentationInfo("Set content placements command handler", "İçerik öğesinin yerleşim (surface/slot) setini değiştirir.")]
public sealed class SetContentPlacementsBffCommandHandler
    : AizenCommandHandler<SetContentPlacementsBffCommand, ContentItemBffDto>
{
    private readonly IContentRemoteCall _content;

    public SetContentPlacementsBffCommandHandler(IContentRemoteCall content) => _content = content;

    public override async Task<ContentItemBffDto?> Handle(
        SetContentPlacementsBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _content.SetPlacements(request.Id, request.Body);
        return ContentBffMapper.ToBff(result?.Body);
    }
}
