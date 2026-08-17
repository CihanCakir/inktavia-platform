using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;

namespace Aizen.Modules.Content.Application.Queries.GetMyContentFavorites;

public sealed class GetMyContentFavoritesQueryHandler
    : AizenQueryHandler<GetMyContentFavoritesQuery, ContentFavoritesResponse>
{
    private readonly IContentFavoriteRepository _favorites;
    private readonly IContentItemRepository _items;
    private readonly IAizenInfoAccessor _info;

    public GetMyContentFavoritesQueryHandler(
        IContentFavoriteRepository favorites, IContentItemRepository items, IAizenInfoAccessor info)
    {
        _favorites = favorites;
        _items = items;
        _info = info;
    }

    public override async Task<ContentFavoritesResponse> Handle(
        GetMyContentFavoritesQuery request, CancellationToken cancellationToken)
    {
        var userId = _info.UserInfoAccessor?.UserInfo?.UserId ?? 0;
        if (userId <= 0)
            throw new AizenBusinessException("Authenticated participant identity could not be resolved.");

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var lang = string.IsNullOrWhiteSpace(request.Lang) ? "tr" : request.Lang;

        var total = await _favorites.CountByUserAsync(userId, cancellationToken);
        var favorites = await _favorites.GetByUserAsync(userId, (page - 1) * pageSize, pageSize, cancellationToken);

        // Batch-fetch the favorited items in ONE query (C9: replaces the per-favorite N+1), then re-associate.
        var itemsById = (await _items.GetByIdsAsync(favorites.Select(f => f.ContentId), cancellationToken))
            .ToDictionary(d => d.Id);

        var items = favorites.Select(fav =>
        {
            var dto = ContentMapper.ToDto(fav);
            dto.Content = itemsById.TryGetValue(fav.ContentId, out var item)
                ? ContentMapper.ToSummary(item, lang)
                : null;
            return dto;
        }).ToList();

        return new ContentFavoritesResponse { Items = items, Page = page, PageSize = pageSize, Total = total };
    }
}
