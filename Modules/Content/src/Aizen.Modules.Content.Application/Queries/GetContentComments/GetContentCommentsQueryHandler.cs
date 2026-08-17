using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;

namespace Aizen.Modules.Content.Application.Queries.GetContentComments;

public sealed class GetContentCommentsQueryHandler
    : AizenQueryHandler<GetContentCommentsQuery, ContentCommentsResponse>
{
    private readonly IContentCommentRepository _comments;

    public GetContentCommentsQueryHandler(IContentCommentRepository comments) => _comments = comments;

    public override async Task<ContentCommentsResponse> Handle(
        GetContentCommentsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var total = await _comments.CountByContentAsync(request.ContentId, ContentCommentStatus.Approved, cancellationToken);
        var docs = await _comments.GetByContentAsync(
            request.ContentId, ContentCommentStatus.Approved, (page - 1) * pageSize, pageSize, cancellationToken);

        return new ContentCommentsResponse
        {
            Items = docs.Select(ContentMapper.ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            Total = total,
        };
    }
}
