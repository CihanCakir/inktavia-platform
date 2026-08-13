using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;

namespace Aizen.Modules.Content.Application.Commands.AttachContentMedia;

public sealed class AttachContentMediaCommandHandler
    : AizenCommandHandler<AttachContentMediaCommand, ContentItemDto>
{
    public override bool IsTransactional => false;

    private readonly IContentItemRepository _items;
    private readonly IContentCacheInvalidator _cache;
    private readonly IContentMediaValidator _media;

    public AttachContentMediaCommandHandler(
        IContentItemRepository items, IContentCacheInvalidator cache, IContentMediaValidator media)
    {
        _items = items;
        _cache = cache;
        _media = media;
    }

    public override async Task<ContentItemDto?> Handle(
        AttachContentMediaCommand request, CancellationToken cancellationToken)
    {
        if (request.Media.Count > 0)
            await _media.ValidateAsync(request.Media, cancellationToken);   // B3 media validation

        var item = await _items.GetByIdAsync(request.ContentId, cancellationToken)
            ?? throw new AizenBusinessException($"Content '{request.ContentId}' was not found.");

        ContentStatusTransition.EnsureEditable(item.Status);

        item.Media = request.Media.Select(ContentMapper.ToVo).ToList();
        item.UpdatedAt = DateTimeOffset.UtcNow;

        await _items.ReplaceAsync(item, cancellationToken);

        if (item.Status == ContentStatus.Published)
            await _cache.BumpAsync(item.Placements.Select(p => p.Surface), cancellationToken);

        return ContentMapper.ToDto(item);
    }
}
