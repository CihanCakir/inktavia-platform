using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

[DocumentationInfo("Create content category command handler", "Editöryel kategori oluşturur.")]
public sealed class CreateContentCategoryBffCommandHandler
    : AizenCommandHandler<CreateContentCategoryBffCommand, ContentCategoryBffDto>
{
    private readonly IContentRemoteCall _content;

    public CreateContentCategoryBffCommandHandler(IContentRemoteCall content) => _content = content;

    public override async Task<ContentCategoryBffDto?> Handle(
        CreateContentCategoryBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _content.CreateCategory(request.Body);
        return result?.Body is null ? null : ContentBffMapper.ToBff(result.Body);
    }
}
