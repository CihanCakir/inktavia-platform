using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;
using Aizen.Modules.Content.Domain.Interface.Service;
using Aizen.Modules.Content.Domain.MongoDocuments;

namespace Aizen.Modules.Content.Application.Commands.CreateContentCategory;

public sealed class CreateContentCategoryCommandHandler
    : AizenCommandHandler<CreateContentCategoryCommand, ContentCategoryDto>
{
    public override bool IsTransactional => false;

    private readonly IContentCategoryRepository _categories;
    private readonly ISlugService _slug;
    private readonly IAizenInfoAccessor _info;
    private readonly IContentCacheInvalidator _cache;

    public CreateContentCategoryCommandHandler(
        IContentCategoryRepository categories, ISlugService slug, IAizenInfoAccessor info, IContentCacheInvalidator cache)
    {
        _categories = categories;
        _slug = slug;
        _info = info;
        _cache = cache;
    }

    public override async Task<ContentCategoryDto?> Handle(
        CreateContentCategoryCommand request, CancellationToken cancellationToken)
    {
        ContentAuthorization.EnsureElevated(_info, "Create content category");

        var slug = _slug.Slugify(request.Slug);
        if (await _categories.SlugExistsAsync(slug, excludeId: null, cancellationToken))
            throw new AizenBusinessException($"Category slug '{slug}' is already in use.");

        var document = new ContentCategoryDocument
        {
            Slug = slug,
            Name = new Dictionary<string, string>(request.Name),
            ParentSlug = request.ParentSlug,
            Position = request.Position,
            IsActive = request.IsActive,
        };

        await _categories.AddAsync(document, cancellationToken);

        // Bump the global generation so the public category tree refreshes immediately (C9: fixes the
        // C5 TTL-only staleness). No surface generation is bumped — categories are not surface-scoped.
        await _cache.BumpAsync(Array.Empty<Aizen.Modules.Content.Abstraction.Enum.ContentSurface>(), cancellationToken);

        return ContentMapper.ToDto(document);
    }
}
