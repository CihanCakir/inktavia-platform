using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

[DocumentationInfo("Moderate content comment command handler", "Bir yorumu moderasyondan geçirir (Approved/Rejected/Hidden).")]
public sealed class ModerateContentCommentBffCommandHandler
    : AizenCommandHandler<ModerateContentCommentBffCommand, ContentCommentBffDto>
{
    private readonly IContentRemoteCall _content;

    public ModerateContentCommentBffCommandHandler(IContentRemoteCall content) => _content = content;

    public override async Task<ContentCommentBffDto?> Handle(
        ModerateContentCommentBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _content.ModerateComment(request.CommentId, request.Body);
        return result?.Body is null ? null : ContentBffMapper.ToBff(result.Body);
    }
}
