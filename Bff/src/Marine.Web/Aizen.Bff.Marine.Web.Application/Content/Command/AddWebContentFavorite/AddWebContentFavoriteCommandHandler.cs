using System.Net;
using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Web.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Content.Command.AddWebContentFavorite;

public sealed class AddWebContentFavoriteCommandHandler
    : AizenCommandHandler<AddWebContentFavoriteCommand, ContentFavoriteResultDto>
{
    private readonly IWebParticipantProfileResolver _resolver;
    private readonly IContentRemoteCall _content;
    private readonly ILogger<AddWebContentFavoriteCommandHandler> _logger;

    public AddWebContentFavoriteCommandHandler(
        IWebParticipantProfileResolver resolver, IContentRemoteCall content, ILogger<AddWebContentFavoriteCommandHandler> logger)
    {
        _resolver = resolver;
        _content = content;
        _logger = logger;
    }

    public override async Task<ContentFavoriteResultDto?> Handle(
        AddWebContentFavoriteCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.UserId is not > 0)
            throw new AizenBusinessException("No participant identity is linked to this account yet.");

        try
        {
            // ContentFavoriteResultDto is clean (ContentId/Favorited/FavoriteCount) — passed through.
            var resp = await _content.AddFavorite(request.ContentId);
            return resp?.Body ?? throw new AizenBusinessException("Could not update the favorite.");
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new AizenBusinessException("Content not found.");
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Add web content favorite failed for {ContentId} (status {Status}).",
                request.ContentId, ex.StatusCode);
            throw new AizenBusinessException("Could not update the favorite.");
        }
    }
}
