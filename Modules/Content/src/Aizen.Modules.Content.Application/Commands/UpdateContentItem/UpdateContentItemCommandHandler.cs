using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;
using Aizen.Modules.Content.Domain.Interface.Service;

namespace Aizen.Modules.Content.Application.Commands.UpdateContentItem;

public sealed class UpdateContentItemCommandHandler
    : AizenCommandHandler<UpdateContentItemCommand, ContentItemDto>
{
    public override bool IsTransactional => false;

    private readonly IContentItemRepository _items;
    private readonly ISlugService _slug;
    private readonly IContentCacheInvalidator _cache;
    private readonly ILanguageValidator _language;

    public UpdateContentItemCommandHandler(
        IContentItemRepository items, ISlugService slug, IContentCacheInvalidator cache, ILanguageValidator language)
    {
        _items = items;
        _slug = slug;
        _cache = cache;
        _language = language;
    }

    public override async Task<ContentItemDto?> Handle(
        UpdateContentItemCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.DefaultLanguage))
            _language.EnsureSupported(request.DefaultLanguage);   // B4 language validation

        var item = await _items.GetByIdAsync(request.ContentId, cancellationToken)
            ?? throw new AizenBusinessException($"Content '{request.ContentId}' was not found.");

        ContentStatusTransition.EnsureEditable(item.Status);

        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            var newSlug = _slug.Slugify(request.Slug);
            if (!string.Equals(newSlug, item.Slug, StringComparison.Ordinal))
            {
                if (await _items.SlugExistsAsync(newSlug, excludeId: item.Id, cancellationToken))
                    throw new AizenBusinessException($"Slug '{newSlug}' is already in use.");
                item.Slug = newSlug;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.DefaultLanguage))
            item.DefaultLanguage = request.DefaultLanguage;

        if (request.Tags is not null)
            item.Tags = new List<string>(request.Tags);

        if (request.ClearCategory)
            item.CategorySlug = null;
        else if (request.CategorySlug is not null)
            item.CategorySlug = request.CategorySlug;

        if (request.Campaign is not null)
            item.Campaign = item.Type == ContentType.Campaign ? ContentMapper.ToVo(request.Campaign) : item.Campaign;

        if (request.ReleaseNote is not null)
            item.ReleaseNote = item.Type == ContentType.ReleaseNote ? ContentMapper.ToVo(request.ReleaseNote) : item.ReleaseNote;

        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _items.ReplaceAsync(item, cancellationToken);

        if (item.Status == ContentStatus.Published)
            await _cache.BumpAsync(item.Placements.Select(p => p.Surface), cancellationToken);

        return ContentMapper.ToDto(item);
    }
}
