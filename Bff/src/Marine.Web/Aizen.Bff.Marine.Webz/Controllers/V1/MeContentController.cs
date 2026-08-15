using Aizen.Bff.Marine.Web.Application.Common.Authorization;
using Aizen.Bff.Marine.Web.Application.Content.Command.AddWebContentComment;
using Aizen.Bff.Marine.Web.Application.Content.Command.AddWebContentFavorite;
using Aizen.Bff.Marine.Web.Application.Content.Command.RemoveWebContentFavorite;
using Aizen.Bff.Marine.Web.Application.Content.Query.GetWebMyContentComments;
using Aizen.Bff.Marine.Web.Application.Content.Query.GetWebMyContentFavorites;
using Aizen.Bff.Marine.Web.Application.Contracts.Content;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.Marine.Web.Controllers.V1;

/// <summary>
/// Authenticated website participant engagement — comment / favorite on content. Author identity is never in the
/// body; it is resolved from the verified token and asserted to Content by the BFF. Anonymous callers are rejected.
/// Thin: every action dispatches a command/query.
/// </summary>
[ApiController]
[Route("api/v1/web/me/content")]
[Tags("Web - Me Content")]
[Authorize(Policy = WebAuthorizationPolicies.WebAuthenticated)]
public sealed class MeContentController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public MeContentController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
        => _cqrs = cqrs;

    [HttpPost("items/{id}/comments")]
    public async Task<AizenApiResponse<WebMyCommentDto?>> AddComment(
        string id, [FromBody] AddContentCommentRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new AddWebContentCommentCommand
        {
            ContentId = id, Body = body.Body, ParentCommentId = body.ParentCommentId,
        }, ct);
        return SetResponse(result);
    }

    [HttpPost("items/{id}/favorite")]
    public async Task<AizenApiResponse<ContentFavoriteResultDto?>> AddFavorite(string id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new AddWebContentFavoriteCommand { ContentId = id }, ct);
        return SetResponse(result);
    }

    [HttpDelete("items/{id}/favorite")]
    public async Task<AizenApiResponse<ContentFavoriteResultDto?>> RemoveFavorite(string id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new RemoveWebContentFavoriteCommand { ContentId = id }, ct);
        return SetResponse(result);
    }

    [HttpGet("favorites")]
    public async Task<AizenApiResponse<WebMyFavoritesResponse?>> MyFavorites(
        [FromQuery] string lang = "tr",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetWebMyContentFavoritesQuery { Lang = lang, Page = page, PageSize = pageSize }, ct);
        return SetResponse(result);
    }

    [HttpGet("items/{id}/my-comments")]
    public async Task<AizenApiResponse<List<WebMyCommentDto>?>> MyComments(string id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetWebMyContentCommentsQuery { ContentId = id }, ct);
        return SetResponse(result);
    }
}
