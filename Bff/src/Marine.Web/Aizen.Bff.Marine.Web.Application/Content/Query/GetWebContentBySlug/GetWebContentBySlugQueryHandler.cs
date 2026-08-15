using System.Net;
using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Web.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentBySlug;

public sealed class GetWebContentBySlugQueryHandler
    : AizenQueryHandler<GetWebContentBySlugQuery, WebContentDetailDto>
{
    private readonly IContentRemoteCall _content;
    private readonly ILogger<GetWebContentBySlugQueryHandler> _logger;

    public GetWebContentBySlugQueryHandler(IContentRemoteCall content, ILogger<GetWebContentBySlugQueryHandler> logger)
    {
        _content = content;
        _logger = logger;
    }

    public override async Task<WebContentDetailDto?> Handle(
        GetWebContentBySlugQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // The module returns the raw ContentItemDto (or 404 when not visible); reshaped to the web detail DTO
            // (translations resolved to lang; internal fields omitted).
            var dto = await _content.GetPublicBySlug(request.Slug, request.Lang);
            return WebContentMapper.ToDetail(dto, request.Lang);
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new AizenBusinessException("Content not found.");
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Content public by-slug failed for '{Slug}' (status {Status}).", request.Slug, ex.StatusCode);
            throw new AizenBusinessException("The content is currently unavailable.");
        }
    }
}
