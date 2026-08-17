using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;

namespace Aizen.Modules.Content.Application.Queries.GetMyCommentStatus;

public sealed class GetMyCommentStatusQueryHandler
    : AizenQueryHandler<GetMyCommentStatusQuery, List<ContentCommentDto>>
{
    private readonly IContentCommentRepository _comments;
    private readonly IAizenInfoAccessor _info;

    public GetMyCommentStatusQueryHandler(IContentCommentRepository comments, IAizenInfoAccessor info)
    {
        _comments = comments;
        _info = info;
    }

    public override async Task<List<ContentCommentDto>> Handle(
        GetMyCommentStatusQuery request, CancellationToken cancellationToken)
    {
        var userId = _info.UserInfoAccessor?.UserInfo?.UserId ?? 0;
        if (userId <= 0)
            throw new AizenBusinessException("Authenticated participant identity could not be resolved.");

        var comments = await _comments.GetByContentAndAuthorAsync(request.ContentId, userId, cancellationToken);
        return comments.Select(ContentMapper.ToDto).ToList();
    }
}
