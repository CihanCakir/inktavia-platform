using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;
using Aizen.Modules.Content.Domain.MongoDocuments;
using Microsoft.Extensions.Configuration;

namespace Aizen.Modules.Content.Application.Commands.AddContentComment;

public sealed class AddContentCommentCommandHandler
    : AizenCommandHandler<AddContentCommentCommand, ContentCommentDto>
{
    public override bool IsTransactional => false;

    private readonly IContentItemRepository _items;
    private readonly IContentCommentRepository _comments;
    private readonly IAizenInfoAccessor _info;
    private readonly IConfiguration _configuration;

    public AddContentCommentCommandHandler(
        IContentItemRepository items,
        IContentCommentRepository comments,
        IAizenInfoAccessor info,
        IConfiguration configuration)
    {
        _items = items;
        _comments = comments;
        _info = info;
        _configuration = configuration;
    }

    public override async Task<ContentCommentDto?> Handle(
        AddContentCommentCommand request, CancellationToken cancellationToken)
    {
        var userId = _info.UserInfoAccessor?.UserInfo?.UserId ?? 0;
        if (userId <= 0)
            throw new AizenBusinessException("Authenticated participant identity could not be resolved.");

        var item = await _items.GetByIdAsync(request.ContentId, cancellationToken);
        if (item is null || !ContentVisibility.IsEngageable(item, DateTimeOffset.UtcNow))
            throw new AizenBusinessException("Content is not available for commenting.");

        // Pre-moderation flag (default ON): new comments start Pending unless disabled by config.
        var preModeration = _configuration.GetValue("Content:Comments:PreModeration", true);
        var status = preModeration ? ContentCommentStatus.Pending : ContentCommentStatus.Approved;

        var now = DateTimeOffset.UtcNow;
        var comment = new ContentCommentDocument
        {
            ContentId = request.ContentId,
            AuthorUserId = userId,
            AuthorProfileId = null,      // participant profile id is not surfaced by the accessor (§4.7)
            AuthorDisplayName = null,    // resolved by the BFF/Identity when rendering; not stored here
            Body = request.Body,
            Status = status,
            ParentCommentId = request.ParentCommentId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _comments.AddAsync(comment, cancellationToken);

        // CommentCount tracks APPROVED comments only (what the public sees). A Pending comment increments
        // it later when a moderator approves it (C7).
        if (status == ContentCommentStatus.Approved)
            await _items.IncrementCommentCountAsync(request.ContentId, 1, cancellationToken);

        return ContentMapper.ToDto(comment);
    }
}
