using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;

namespace Aizen.Modules.Content.Application.Queries.GetContentByIdAdmin;

public sealed class GetContentByIdAdminQueryHandler
    : AizenQueryHandler<GetContentByIdAdminQuery, ContentItemDto?>
{
    private readonly IContentItemRepository _items;

    public GetContentByIdAdminQueryHandler(IContentItemRepository items) => _items = items;

    public override async Task<ContentItemDto?> Handle(
        GetContentByIdAdminQuery request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ContentId, cancellationToken);
        return item is null ? null : ContentMapper.ToDto(item);
    }
}
