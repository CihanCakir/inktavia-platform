using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;
using Aizen.Modules.Content.Domain.Interface.Service;
using Aizen.Modules.Content.Domain.MongoDocuments;

namespace Aizen.Modules.Content.Application.Commands.CreateContentItem;

public sealed class CreateContentItemCommandHandler
    : AizenCommandHandler<CreateContentItemCommand, ContentItemDto>
{
    // Mongo-only module: no UnitOfWork is registered, so the transactional decorator is a no-op.
    public override bool IsTransactional => false;

    private readonly IContentItemRepository _items;
    private readonly ISlugService _slug;
    private readonly ILanguageValidator _language;
    private readonly IContentMediaValidator _media;

    public CreateContentItemCommandHandler(
        IContentItemRepository items, ISlugService slug, ILanguageValidator language, IContentMediaValidator media)
    {
        _items = items;
        _slug = slug;
        _language = language;
        _media = media;
    }

    public override async Task<ContentItemDto?> Handle(
        CreateContentItemCommand request, CancellationToken cancellationToken)
    {
        // Cross-module validation (B4 languages, B3 media) before any write.
        _language.EnsureSupported(request.DefaultLanguage);
        foreach (var translation in request.Translations)
            _language.EnsureSupported(translation.Lang);
        if (request.Media.Count > 0)
            await _media.ValidateAsync(request.Media, cancellationToken);

        var defaultTitle = request.Translations
            .FirstOrDefault(t => string.Equals(t.Lang, request.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
            ?.Title;

        string slug;
        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            slug = await _slug.GenerateUniqueSlugAsync(defaultTitle ?? request.Type.ToString(), cancellationToken);
        }
        else
        {
            slug = _slug.Slugify(request.Slug);
            if (await _items.SlugExistsAsync(slug, excludeId: null, cancellationToken))
                throw new AizenBusinessException($"Slug '{slug}' is already in use.");
        }

        var now = DateTimeOffset.UtcNow;
        var document = new ContentItemDocument
        {
            Type = request.Type,
            Slug = slug,
            Status = ContentStatus.Draft,
            DefaultLanguage = request.DefaultLanguage,
            Translations = request.Translations.Select(ContentMapper.ToVo).ToList(),
            Media = request.Media.Select(ContentMapper.ToVo).ToList(),
            Placements = request.Placements.Select(ContentMapper.ToVo).ToList(),
            Audience = request.Audience is null ? new Domain.ValueObjects.ContentAudience() : ContentMapper.ToVo(request.Audience),
            Tags = new List<string>(request.Tags),
            CategorySlug = request.CategorySlug,
            Campaign = request.Type == ContentType.Campaign && request.Campaign is not null
                ? ContentMapper.ToVo(request.Campaign) : null,
            ReleaseNote = request.Type == ContentType.ReleaseNote && request.ReleaseNote is not null
                ? ContentMapper.ToVo(request.ReleaseNote) : null,
            PublishAt = request.PublishAt,
            ExpireAt = request.ExpireAt,
            AuthorUserId = request.AuthorUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _items.AddAsync(document, cancellationToken);
        return ContentMapper.ToDto(document);
    }
}
