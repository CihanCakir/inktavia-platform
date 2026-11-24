using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

[DocumentationInfo("Create content item command handler", "Content modülünde taslak içerik oluşturur.")]
public sealed class CreateContentItemBffCommandHandler
    : AizenCommandHandler<CreateContentItemBffCommand, ContentItemBffDto>
{
    private readonly IContentRemoteCall _content;

    public CreateContentItemBffCommandHandler(IContentRemoteCall content) => _content = content;

    public override async Task<ContentItemBffDto?> Handle(
        CreateContentItemBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _content.CreateItem(request.Body);
        return ContentBffMapper.ToBff(result?.Body);
    }
}
