using System.Net;
using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Web.Application.Common.Services;
using Aizen.Bff.Marine.Web.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Model;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Content.Command.AddWebContentComment;

public sealed class AddWebContentCommentCommandHandler
    : AizenCommandHandler<AddWebContentCommentCommand, WebMyCommentDto>
{
    private readonly IWebParticipantProfileResolver _resolver;
    private readonly IContentRemoteCall _content;
    private readonly ILogger<AddWebContentCommentCommandHandler> _logger;

    public AddWebContentCommentCommandHandler(
        IWebParticipantProfileResolver resolver, IContentRemoteCall content, ILogger<AddWebContentCommentCommandHandler> logger)
    {
        _resolver = resolver;
        _content = content;
        _logger = logger;
    }

    public override async Task<WebMyCommentDto?> Handle(
        AddWebContentCommentCommand request, CancellationToken cancellationToken)
    {
        // Resolve identity FIRST → populates the holder so the AddComment call below carries the assertion headers
        // (Content derives UserInfo.UserId from X-Aizen-User-Id). The resolver's own Identity call runs before the
        // holder is set → no assertion → no recursion.
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.UserId is not > 0)
            throw new AizenBusinessException("No participant identity is linked to this account yet.");

        try
        {
            var resp = await _content.AddComment(request.ContentId,
                new AddContentCommentRequest { Body = request.Body, ParentCommentId = request.ParentCommentId });
            var dto = resp?.Body ?? throw new AizenBusinessException("Could not add the comment.");
            return WebContentMapper.ToMyComment(dto);
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new AizenBusinessException("Content not found.");
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Add web content comment failed for {ContentId} (status {Status}).",
                request.ContentId, ex.StatusCode);
            throw new AizenBusinessException("Could not add the comment.");
        }
    }
}
