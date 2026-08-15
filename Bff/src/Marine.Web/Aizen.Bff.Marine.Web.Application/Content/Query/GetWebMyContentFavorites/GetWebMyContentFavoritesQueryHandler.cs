using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Web.Application.Common.Services;
using Aizen.Bff.Marine.Web.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Content.Query.GetWebMyContentFavorites;

public sealed class GetWebMyContentFavoritesQueryHandler
    : AizenQueryHandler<GetWebMyContentFavoritesQuery, WebMyFavoritesResponse>
{
    private readonly IWebParticipantProfileResolver _resolver;
    private readonly IContentRemoteCall _content;
    private readonly ILogger<GetWebMyContentFavoritesQueryHandler> _logger;

    public GetWebMyContentFavoritesQueryHandler(
        IWebParticipantProfileResolver resolver, IContentRemoteCall content, ILogger<GetWebMyContentFavoritesQueryHandler> logger)
    {
        _resolver = resolver;
        _content = content;
        _logger = logger;
    }

    public override async Task<WebMyFavoritesResponse?> Handle(
        GetWebMyContentFavoritesQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.UserId is not > 0)
            throw new AizenBusinessException("No participant identity is linked to this account yet.");

        try
        {
            var resp = await _content.GetMyFavorites(request.Lang, request.Page, request.PageSize);
            var body = resp?.Body ?? throw new AizenBusinessException("Your favorites are currently unavailable.");
            // Reshaped to strip the participant's internal UserId/ProfileId; content summary passes through.
            return WebContentMapper.ToMyFavorites(body);
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Get web my-favorites failed (status {Status}).", ex.StatusCode);
            throw new AizenBusinessException("Your favorites are currently unavailable.");
        }
    }
}
