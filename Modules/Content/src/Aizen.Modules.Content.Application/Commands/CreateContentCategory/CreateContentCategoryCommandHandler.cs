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

    public CreateContentCategoryCommandHandler(
        IContentCategoryRepository categories, ISlugService slug, IAizenInfoAccessor info)
    {
        _categories = categories;
        _slug = slug;
        _info = info;
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
        return ContentMapper.ToDto(document);
    }
}
