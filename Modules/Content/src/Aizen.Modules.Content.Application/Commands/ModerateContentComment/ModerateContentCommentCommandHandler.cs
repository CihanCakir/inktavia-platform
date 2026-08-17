using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
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
    private readonly IAizenInfoAccessor _info;

    public ModerateContentCommentCommandHandler(
        IContentCommentRepository comments, IContentItemRepository items, IAizenInfoAccessor info)
    {
        _comments = comments;
        _items = items;
        _info = info;
    }

    public override async Task<ContentCommentDto?> Handle(
        ModerateContentCommentCommand request, CancellationToken cancellationToken)
    {
        if (request.Status == ContentCommentStatus.Pending)
            throw new AizenBusinessException("Pending is not a valid moderation target (use Approved, Rejected or Hidden).");

        var comment = await _comments.GetByIdAsync(request.CommentId, cancellationToken)
            ?? throw new AizenBusinessException($"Comment '{request.CommentId}' was not found.");

        var delta = ContentCommentCounter.TransitionDelta(comment.Status, request.Status);

        var now = DateTimeOffset.UtcNow;
        comment.Status = request.Status;
        comment.ModeratedByUserId = _info.UserInfoAccessor?.UserInfo?.UserId;
        comment.ModeratedAt = now;
        comment.LastModerationReason = request.Reason;
        comment.UpdatedAt = now;
        await _comments.ReplaceAsync(comment, cancellationToken);

        if (delta != 0)
            await _items.IncrementCommentCountAsync(comment.ContentId, delta, cancellationToken);

        return ContentMapper.ToDto(comment);
    }
}
