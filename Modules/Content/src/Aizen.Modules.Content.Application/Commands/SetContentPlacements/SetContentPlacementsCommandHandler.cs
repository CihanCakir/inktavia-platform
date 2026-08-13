using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;

namespace Aizen.Modules.Content.Application.Commands.SetContentPlacements;

public sealed class SetContentPlacementsCommandHandler
    : AizenCommandHandler<SetContentPlacementsCommand, ContentItemDto>
{
    public override bool IsTransactional => false;

    private readonly IContentItemRepository _items;
    private readonly IContentCacheInvalidator _cache;

    public SetContentPlacementsCommandHandler(IContentItemRepository items, IContentCacheInvalidator cache)
    {
        _items = items;
        _cache = cache;
    }

    public override async Task<ContentItemDto?> Handle(
        SetContentPlacementsCommand request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ContentId, cancellationToken)
            ?? throw new AizenBusinessException($"Content '{request.ContentId}' was not found.");

        ContentStatusTransition.EnsureEditable(item.Status);

        var previousSurfaces = item.Placements.Select(p => p.Surface).ToList();
        item.Placements = request.Placements.Select(ContentMapper.ToVo).ToList();
        item.UpdatedAt = DateTimeOffset.UtcNow;

        await _items.ReplaceAsync(item, cancellationToken);

        if (item.Status == ContentStatus.Published)
            await _cache.BumpAsync(previousSurfaces.Concat(item.Placements.Select(p => p.Surface)), cancellationToken);

        return ContentMapper.ToDto(item);
    }
}
