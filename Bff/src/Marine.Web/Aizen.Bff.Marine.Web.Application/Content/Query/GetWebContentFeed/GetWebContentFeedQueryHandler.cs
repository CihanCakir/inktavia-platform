using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentFeed;

public sealed class GetWebContentFeedQueryHandler
    : AizenQueryHandler<GetWebContentFeedQuery, ContentFeedResponse>
{
    private readonly IContentRemoteCall _content;
    private readonly ILogger<GetWebContentFeedQueryHandler> _logger;

    public GetWebContentFeedQueryHandler(IContentRemoteCall content, ILogger<GetWebContentFeedQueryHandler> logger)
    {
        _content = content;
        _logger = logger;
    }

    public override async Task<ContentFeedResponse?> Handle(
        GetWebContentFeedQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Surface pinned to MarineOsWeb by the BFF — the module returns raw ContentFeedResponse (web-appropriate,
            // passed through unmapped).
            return await _content.GetPublicFeed(WebContentSurface.Pinned, request.Lang, request.Page, request.PageSize);
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Content public feed failed (status {Status}).", ex.StatusCode);
            throw new AizenBusinessException("The content feed is currently unavailable.");
        }
    }
}
