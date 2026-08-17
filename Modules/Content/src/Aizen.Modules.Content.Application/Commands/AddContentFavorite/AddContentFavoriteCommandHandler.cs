using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;
using Aizen.Modules.Content.Domain.MongoDocuments;

namespace Aizen.Modules.Content.Application.Commands.AddContentFavorite;

public sealed class AddContentFavoriteCommandHandler
    : AizenCommandHandler<AddContentFavoriteCommand, ContentFavoriteResultDto>
{
    public override bool IsTransactional => false;

    private readonly IContentItemRepository _items;
    private readonly IContentFavoriteRepository _favorites;
    private readonly IAizenInfoAccessor _info;

    public AddContentFavoriteCommandHandler(
        IContentItemRepository items, IContentFavoriteRepository favorites, IAizenInfoAccessor info)
    {
        _items = items;
        _favorites = favorites;
        _info = info;
    }

    public override async Task<ContentFavoriteResultDto?> Handle(
        AddContentFavoriteCommand request, CancellationToken cancellationToken)
    {
        var userId = _info.UserInfoAccessor?.UserInfo?.UserId ?? 0;
        if (userId <= 0)
            throw new AizenBusinessException("Authenticated participant identity could not be resolved.");

        var item = await _items.GetByIdAsync(request.ContentId, cancellationToken);
        if (item is null || !ContentVisibility.IsEngageable(item, DateTimeOffset.UtcNow))
            throw new AizenBusinessException("Content is not available for favoriting.");

        var favorite = new ContentFavoriteDocument
        {
            ContentId = request.ContentId,
            UserId = userId,
            ProfileId = null,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var added = await _favorites.TryAddAsync(favorite, cancellationToken);
        if (added)
            await _items.IncrementFavoriteCountAsync(request.ContentId, 1, cancellationToken);

        return new ContentFavoriteResultDto
        {
            ContentId = request.ContentId,
            Favorited = true,
            FavoriteCount = item.FavoriteCount + (added ? 1 : 0),
        };
    }
}
