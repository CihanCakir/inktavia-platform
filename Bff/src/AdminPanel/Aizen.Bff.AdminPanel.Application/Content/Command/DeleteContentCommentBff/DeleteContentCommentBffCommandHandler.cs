using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

[DocumentationInfo("Delete content comment command handler", "Bir yorumu siler.")]
public sealed class DeleteContentCommentBffCommandHandler
    : AizenCommandHandler<DeleteContentCommentBffCommand, ContentCommentBffDto>
{
    private readonly IContentRemoteCall _content;

    public DeleteContentCommentBffCommandHandler(IContentRemoteCall content) => _content = content;

    public override async Task<ContentCommentBffDto?> Handle(
        DeleteContentCommentBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _content.DeleteComment(request.CommentId);
        return result?.Body is null ? null : ContentBffMapper.ToBff(result.Body);
    }
}
