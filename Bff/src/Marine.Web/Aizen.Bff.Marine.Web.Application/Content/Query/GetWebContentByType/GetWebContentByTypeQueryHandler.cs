using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentByType;

public sealed class GetWebContentByTypeQueryHandler
    : AizenQueryHandler<GetWebContentByTypeQuery, ContentFeedResponse>
{
    private readonly IContentRemoteCall _content;
    private readonly ILogger<GetWebContentByTypeQueryHandler> _logger;

    public GetWebContentByTypeQueryHandler(IContentRemoteCall content, ILogger<GetWebContentByTypeQueryHandler> logger)
    {
        _content = content;
        _logger = logger;
    }

    public override async Task<ContentFeedResponse?> Handle(
        GetWebContentByTypeQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Surface pinned to MarineOsWeb by the BFF; raw ContentFeedResponse passed through unmapped.
            return await _content.GetPublicByType(
                WebContentSurface.Pinned, request.Type, request.Lang, request.Page, request.PageSize);
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Content public by-type failed for {Type} (status {Status}).", request.Type, ex.StatusCode);
            throw new AizenBusinessException("The content feed is currently unavailable.");
        }
    }
}
