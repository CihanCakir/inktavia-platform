using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Model;
using Aizen.Modules.Content.Application.Commands.AddContentComment;
using Aizen.Modules.Content.Application.Commands.AddContentFavorite;
using Aizen.Modules.Content.Application.Commands.RemoveContentFavorite;
using Aizen.Modules.Content.Application.Queries.GetMyCommentStatus;
using Aizen.Modules.Content.Application.Queries.GetMyContentFavorites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Content.Controllers.V1;

/// <summary>
/// Participant engagement surface (§7). Any authenticated app user; author identity is taken from the
/// token in the handlers, never from the body. Thin — every action dispatches a command/query.
/// </summary>
[ApiController]
[Route("api/v1/content/me")]
[Tags("Content - Me")]
[Authorize]
public sealed class MeContentController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public MeContentController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs, IAizenInfoAccessor info)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
        _ = info; // identity is resolved inside the handlers via IAizenInfoAccessor
    }

    [HttpPost("items/{contentId}/comments")]
    public async Task<AizenApiResponse<ContentCommentDto?>> AddComment(
        string contentId, [FromBody] AddContentCommentRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentCommentDto>(new AddContentCommentCommand
        {
            ContentId = contentId,
            Body = body.Body,
            ParentCommentId = body.ParentCommentId,
        }, ct);
        return SetResponse(result);
    }

    [HttpPost("items/{contentId}/favorite")]
    public async Task<AizenApiResponse<ContentFavoriteResultDto?>> AddFavorite(
        string contentId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentFavoriteResultDto>(
            new AddContentFavoriteCommand { ContentId = contentId }, ct);
        return SetResponse(result);
    }

    [HttpDelete("items/{contentId}/favorite")]
    public async Task<AizenApiResponse<ContentFavoriteResultDto?>> RemoveFavorite(
        string contentId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentFavoriteResultDto>(
            new RemoveContentFavoriteCommand { ContentId = contentId }, ct);
        return SetResponse(result);
    }

    [HttpGet("favorites")]
    public async Task<AizenApiResponse<ContentFavoritesResponse?>> MyFavorites(
        [FromQuery] string lang = "tr",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentFavoritesResponse>(new GetMyContentFavoritesQuery
        {
            Lang = lang, Page = page, PageSize = pageSize,
        }, ct);
        return SetResponse(result);
    }

    [HttpGet("items/{contentId}/my-comments")]
    public async Task<AizenApiResponse<List<ContentCommentDto>?>> MyComments(
        string contentId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<List<ContentCommentDto>>(
            new GetMyCommentStatusQuery { ContentId = contentId }, ct);
        return SetResponse(result);
    }
}
