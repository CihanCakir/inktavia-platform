using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;

namespace Aizen.Modules.Content.Application.Commands.ModerateContentComment;

public sealed class ModerateContentCommentCommandHandler
    : AizenCommandHandler<ModerateContentCommentCommand, ContentCommentDto>
{
    public override bool IsTransactional => false;

    private readonly IContentCommentRepository _comments;
    private readonly IContentItemRepository _items;

    public ModerateContentCommentCommandHandler(IContentCommentRepository comments, IContentItemRepository items)
    {
        _comments = comments;
        _items = items;
    }

    public override async Task<ContentCommentDto?> Handle(
        ModerateContentCommentCommand request, CancellationToken cancellationToken)
    {
        if (request.Status == ContentCommentStatus.Pending)
            throw new AizenBusinessException("Pending is not a valid moderation target (use Approved, Rejected or Hidden).");

        var comment = await _comments.GetByIdAsync(request.CommentId, cancellationToken)
            ?? throw new AizenBusinessException($"Comment '{request.CommentId}' was not found.");

        var delta = ContentCommentCounter.TransitionDelta(comment.Status, request.Status);

        // NOTE: Reason is accepted for the moderator's audit intent; the comment document has no field
        // to persist it (a dedicated moderation log can be added in C9).
        comment.Status = request.Status;
        comment.UpdatedAt = DateTimeOffset.UtcNow;
        await _comments.ReplaceAsync(comment, cancellationToken);

        if (delta != 0)
            await _items.IncrementCommentCountAsync(comment.ContentId, delta, cancellationToken);

        return ContentMapper.ToDto(comment);
    }
}
