using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

[DocumentationInfo("Set content audience command handler", "İçerik öğesinin hedef kitle setini değiştirir.")]
public sealed class SetContentAudienceBffCommandHandler
    : AizenCommandHandler<SetContentAudienceBffCommand, ContentItemBffDto>
{
    private readonly IContentRemoteCall _content;

    public SetContentAudienceBffCommandHandler(IContentRemoteCall content) => _content = content;

    public override async Task<ContentItemBffDto?> Handle(
        SetContentAudienceBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _content.SetAudience(request.Id, request.Body);
        return ContentBffMapper.ToBff(result?.Body);
    }
}
