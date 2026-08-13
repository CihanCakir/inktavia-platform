using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Domain.Interface.Repository;

namespace Aizen.Modules.Content.Application.Commands.RemoveContentFavorite;

public sealed class RemoveContentFavoriteCommandHandler
    : AizenCommandHandler<RemoveContentFavoriteCommand, ContentFavoriteResultDto>
{
    public override bool IsTransactional => false;

    private readonly IContentItemRepository _items;
    private readonly IContentFavoriteRepository _favorites;
    private readonly IAizenInfoAccessor _info;

    public RemoveContentFavoriteCommandHandler(
        IContentItemRepository items, IContentFavoriteRepository favorites, IAizenInfoAccessor info)
    {
        _items = items;
        _favorites = favorites;
        _info = info;
    }

    public override async Task<ContentFavoriteResultDto?> Handle(
        RemoveContentFavoriteCommand request, CancellationToken cancellationToken)
    {
        var userId = _info.UserInfoAccessor?.UserInfo?.UserId ?? 0;
        if (userId <= 0)
            throw new AizenBusinessException("Authenticated participant identity could not be resolved.");

        // Removal is allowed regardless of the item's current visibility (a user may un-favorite content
        // that has since been unpublished/expired). The item is loaded only for the response count.
        var item = await _items.GetByIdAsync(request.ContentId, cancellationToken);

        var removed = await _favorites.RemoveAsync(request.ContentId, userId, cancellationToken);
        if (removed && item is not null)
            await _items.IncrementFavoriteCountAsync(request.ContentId, -1, cancellationToken);

        var count = item is null ? 0 : Math.Max(0, item.FavoriteCount - (removed ? 1 : 0));

        return new ContentFavoriteResultDto
        {
            ContentId = request.ContentId,
            Favorited = false,
            FavoriteCount = count,
        };
    }
}
