using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentCategories;

public sealed class GetWebContentCategoriesQueryHandler
    : AizenQueryHandler<GetWebContentCategoriesQuery, List<ContentCategoryDto>>
{
    private readonly IContentRemoteCall _content;
    private readonly ILogger<GetWebContentCategoriesQueryHandler> _logger;

    public GetWebContentCategoriesQueryHandler(IContentRemoteCall content, ILogger<GetWebContentCategoriesQueryHandler> logger)
    {
        _content = content;
        _logger = logger;
    }

    public override async Task<List<ContentCategoryDto>?> Handle(
        GetWebContentCategoriesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Raw List<ContentCategoryDto> — clean taxonomy, passed through unmapped.
            return await _content.GetPublicCategories(request.Lang);
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Content public categories failed (status {Status}).", ex.StatusCode);
            throw new AizenBusinessException("The content categories are currently unavailable.");
        }
    }
}
