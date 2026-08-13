using System.Linq.Expressions;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;
using Aizen.Modules.Content.Domain.MongoDocuments;

namespace Aizen.Modules.Content.Application.Queries.GetAdminContentList;

public sealed class GetAdminContentListQueryHandler
    : AizenQueryHandler<GetAdminContentListQuery, ContentFeedResponse>
{
    private readonly IContentItemRepository _items;

    public GetAdminContentListQueryHandler(IContentItemRepository items) => _items = items;

    public override async Task<ContentFeedResponse> Handle(
        GetAdminContentListQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        ContentType? type = request.Type;
        ContentStatus? status = request.Status;
        ContentSurface? surface = request.Surface;
        var tag = request.Tag;

        Expression<Func<ContentItemDocument, bool>> predicate = x =>
            (!type.HasValue || x.Type == type.Value) &&
            (!status.HasValue || x.Status == status.Value) &&
            (string.IsNullOrEmpty(tag) || x.Tags.Contains(tag)) &&
            (!surface.HasValue || x.Placements.Any(p => p.Surface == surface.Value));

        var total = await _items.CountAsync(predicate, cancellationToken);
        var docs = await _items.FindManyAsync(predicate, (page - 1) * pageSize, pageSize, cancellationToken);

        return new ContentFeedResponse
        {
            Items = docs.Select(d => ContentMapper.ToSummary(d, request.Lang)).ToList(),
            Page = page,
            PageSize = pageSize,
            Total = total,
        };
    }
}
