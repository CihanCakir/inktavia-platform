using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;

namespace Aizen.Modules.Content.Application.Commands.DeleteContentComment;

public sealed class DeleteContentCommentCommandHandler
    : AizenCommandHandler<DeleteContentCommentCommand, ContentCommentDto>
{
    public override bool IsTransactional => false;

    private readonly IContentCommentRepository _comments;
    private readonly IContentItemRepository _items;

    public DeleteContentCommentCommandHandler(IContentCommentRepository comments, IContentItemRepository items)
    {
        _comments = comments;
        _items = items;
    }

    public override async Task<ContentCommentDto?> Handle(
        DeleteContentCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _comments.GetByIdAsync(request.CommentId, cancellationToken)
            ?? throw new AizenBusinessException($"Comment '{request.CommentId}' was not found.");

        var delta = ContentCommentCounter.DeleteDelta(comment.Status);
        var snapshot = ContentMapper.ToDto(comment);

        await _comments.SoftDeleteAsync(comment.Id, cancellationToken);

        if (delta != 0)
            await _items.IncrementCommentCountAsync(comment.ContentId, delta, cancellationToken);

        return snapshot;
    }
}
