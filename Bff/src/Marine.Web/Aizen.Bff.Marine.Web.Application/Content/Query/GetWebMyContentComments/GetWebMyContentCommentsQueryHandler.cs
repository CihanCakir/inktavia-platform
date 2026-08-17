using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Web.Application.Common.Services;
using Aizen.Bff.Marine.Web.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Content.Query.GetWebMyContentComments;

public sealed class GetWebMyContentCommentsQueryHandler
    : AizenQueryHandler<GetWebMyContentCommentsQuery, List<WebMyCommentDto>>
{
    private readonly IWebParticipantProfileResolver _resolver;
    private readonly IContentRemoteCall _content;
    private readonly ILogger<GetWebMyContentCommentsQueryHandler> _logger;

    public GetWebMyContentCommentsQueryHandler(
        IWebParticipantProfileResolver resolver, IContentRemoteCall content, ILogger<GetWebMyContentCommentsQueryHandler> logger)
    {
        _resolver = resolver;
        _content = content;
        _logger = logger;
    }

    public override async Task<List<WebMyCommentDto>?> Handle(
        GetWebMyContentCommentsQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.UserId is not > 0)
            throw new AizenBusinessException("No participant identity is linked to this account yet.");

        try
        {
            var resp = await _content.GetMyComments(request.ContentId);
            var body = resp?.Body ?? new List<Aizen.Modules.Content.Abstraction.Dto.ContentCommentDto>();
            // Own status kept; author ids + moderation trail stripped.
            return body.Select(WebContentMapper.ToMyComment).ToList();
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Get web my-comments failed for {ContentId} (status {Status}).",
                request.ContentId, ex.StatusCode);
            throw new AizenBusinessException("Your comments are currently unavailable.");
        }
    }
}
