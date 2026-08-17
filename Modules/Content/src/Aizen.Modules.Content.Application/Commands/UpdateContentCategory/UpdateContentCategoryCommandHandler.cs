using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;

namespace Aizen.Modules.Content.Application.Commands.UpdateContentCategory;

public sealed class UpdateContentCategoryCommandHandler
    : AizenCommandHandler<UpdateContentCategoryCommand, ContentCategoryDto>
{
    public override bool IsTransactional => false;

    private readonly IContentCategoryRepository _categories;
    private readonly IAizenInfoAccessor _info;
    private readonly IContentCacheInvalidator _cache;

    public UpdateContentCategoryCommandHandler(
        IContentCategoryRepository categories, IAizenInfoAccessor info, IContentCacheInvalidator cache)
    {
        _categories = categories;
        _info = info;
        _cache = cache;
    }

    public override async Task<ContentCategoryDto?> Handle(
        UpdateContentCategoryCommand request, CancellationToken cancellationToken)
    {
        ContentAuthorization.EnsureElevated(_info, "Update content category");

        var category = await _categories.GetBySlugAsync(request.Slug, cancellationToken)
            ?? throw new AizenBusinessException($"Category '{request.Slug}' was not found.");

        category.Name = new Dictionary<string, string>(request.Name);
        category.ParentSlug = request.ParentSlug;
        category.Position = request.Position;
        category.IsActive = request.IsActive;

        await _categories.ReplaceAsync(category, cancellationToken);

        // Bump the global generation so the public category tree refreshes immediately (C9).
        await _cache.BumpAsync(Array.Empty<Aizen.Modules.Content.Abstraction.Enum.ContentSurface>(), cancellationToken);

        return ContentMapper.ToDto(category);
    }
}
