using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;

namespace Aizen.Modules.Content.Application.Queries.GetAdminContentComments;

public sealed class GetAdminContentCommentsQueryHandler
    : AizenQueryHandler<GetAdminContentCommentsQuery, ContentCommentsResponse>
{
    private readonly IContentCommentRepository _comments;

    public GetAdminContentCommentsQueryHandler(IContentCommentRepository comments) => _comments = comments;

    public override async Task<ContentCommentsResponse> Handle(
        GetAdminContentCommentsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var total = await _comments.CountByContentAsync(request.ContentId, request.Status, cancellationToken);
        var docs = await _comments.GetByContentAsync(
            request.ContentId, request.Status, (page - 1) * pageSize, pageSize, cancellationToken);

        return new ContentCommentsResponse
        {
            Items = docs.Select(ContentMapper.ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            Total = total,
        };
    }
}
