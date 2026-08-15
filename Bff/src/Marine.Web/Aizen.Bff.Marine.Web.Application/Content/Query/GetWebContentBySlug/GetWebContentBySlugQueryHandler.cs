using System.Net;
using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Web.Application.Common.Seo;
using Aizen.Bff.Marine.Web.Application.Contracts.Content;
using Aizen.Bff.Marine.Web.Application.Contracts.Seo;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentBySlug;

public sealed class GetWebContentBySlugQueryHandler
    : AizenQueryHandler<GetWebContentBySlugQuery, WebContentDetailDto>
{
    private readonly IContentRemoteCall _content;
    private readonly ISeoIndexabilityPolicy _seo;
    private readonly ILogger<GetWebContentBySlugQueryHandler> _logger;

    public GetWebContentBySlugQueryHandler(
        IContentRemoteCall content, ISeoIndexabilityPolicy seo, ILogger<GetWebContentBySlugQueryHandler> logger)
    {
        _content = content;
        _seo = seo;
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
            var detail = WebContentMapper.ToDetail(dto, request.Lang);

            // W3.2 — backend indexability verdict via the swappable policy seam (thresholds from Options, not here).
            // The public by-slug endpoint only ever returns a Published item, so IsPublished is true.
            var verdict = _seo.Evaluate(new SeoEvaluationInput(
                IsPublished: true,
                Title: detail.Title,
                Description: detail.SeoDescription ?? detail.Summary,
                BodyLength: detail.Body?.Length));
            detail.Seo = new WebSeoDto
            {
                Indexable = verdict.Indexable,
                Reason = verdict.Reason,
                Title = detail.SeoTitle ?? detail.Title,
                Description = detail.SeoDescription ?? detail.Summary,
            };

            return detail;
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
