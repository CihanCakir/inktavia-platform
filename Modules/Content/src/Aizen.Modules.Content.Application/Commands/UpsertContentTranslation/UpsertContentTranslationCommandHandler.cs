using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;
using Aizen.Modules.Content.Domain.ValueObjects;

namespace Aizen.Modules.Content.Application.Commands.UpsertContentTranslation;

public sealed class UpsertContentTranslationCommandHandler
    : AizenCommandHandler<UpsertContentTranslationCommand, ContentItemDto>
{
    public override bool IsTransactional => false;

    private readonly IContentItemRepository _items;
    private readonly IContentCacheInvalidator _cache;
    private readonly ILanguageValidator _language;

    public UpsertContentTranslationCommandHandler(
        IContentItemRepository items, IContentCacheInvalidator cache, ILanguageValidator language)
    {
        _items = items;
        _cache = cache;
        _language = language;
    }

    public override async Task<ContentItemDto?> Handle(
        UpsertContentTranslationCommand request, CancellationToken cancellationToken)
    {
        _language.EnsureSupported(request.Lang);   // B4 language validation

        var item = await _items.GetByIdAsync(request.ContentId, cancellationToken)
            ?? throw new AizenBusinessException($"Content '{request.ContentId}' was not found.");

        ContentStatusTransition.EnsureEditable(item.Status);

        var translation = new ContentTranslation
        {
            Lang = request.Lang,
            Title = request.Title,
            Summary = request.Summary,
            Body = request.Body,
            SeoTitle = request.SeoTitle,
            SeoDescription = request.SeoDescription,
        };

        item.Translations.RemoveAll(t => string.Equals(t.Lang, request.Lang, StringComparison.OrdinalIgnoreCase));
        item.Translations.Add(translation);
        item.UpdatedAt = DateTimeOffset.UtcNow;

        await _items.ReplaceAsync(item, cancellationToken);

        if (item.Status == ContentStatus.Published)
            await _cache.BumpAsync(item.Placements.Select(p => p.Surface), cancellationToken);

        return ContentMapper.ToDto(item);
    }
}
