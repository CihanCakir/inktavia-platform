using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

[DocumentationInfo("Update content category command handler", "Editöryel kategoriyi günceller (slug değişmez).")]
public sealed class UpdateContentCategoryBffCommandHandler
    : AizenCommandHandler<UpdateContentCategoryBffCommand, ContentCategoryBffDto>
{
    private readonly IContentRemoteCall _content;

    public UpdateContentCategoryBffCommandHandler(IContentRemoteCall content) => _content = content;

    public override async Task<ContentCategoryBffDto?> Handle(
        UpdateContentCategoryBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _content.UpdateCategory(request.Slug, request.Body);
        return result?.Body is null ? null : ContentBffMapper.ToBff(result.Body);
    }
}
