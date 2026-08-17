using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Web.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentComments;

public sealed class GetWebContentCommentsQueryHandler
    : AizenQueryHandler<GetWebContentCommentsQuery, WebContentCommentsResponse>
{
    private readonly IContentRemoteCall _content;
    private readonly ILogger<GetWebContentCommentsQueryHandler> _logger;

    public GetWebContentCommentsQueryHandler(IContentRemoteCall content, ILogger<GetWebContentCommentsQueryHandler> logger)
    {
        _content = content;
        _logger = logger;
    }

    public override async Task<WebContentCommentsResponse?> Handle(
        GetWebContentCommentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Module returns raw ContentCommentsResponse; reshaped to strip author ids + moderation trail
            // (must not reach the anonymous public).
            var result = await _content.GetPublicComments(request.ContentId, request.Page, request.PageSize);
            return WebContentMapper.ToComments(result);
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Content public comments failed for {ContentId} (status {Status}).",
                request.ContentId, ex.StatusCode);
            throw new AizenBusinessException("The comments are currently unavailable.");
        }
    }
}
