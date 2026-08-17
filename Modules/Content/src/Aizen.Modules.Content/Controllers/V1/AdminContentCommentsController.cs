using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Abstraction.Model;
using Aizen.Modules.Content.Application.Commands.DeleteContentComment;
using Aizen.Modules.Content.Application.Commands.ModerateContentComment;
using Aizen.Modules.Content.Application.Queries.GetAdminContentComments;
using Aizen.Modules.Content.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Content.Controllers.V1;

/// <summary>
/// Comment moderation surface (§7). Dedicated controller (route api/v1/content/admin/comments) so the
/// ContentModerator role set differs from the authoring surface. Thin — every action dispatches CQRS.
/// </summary>
[ApiController]
[Route("api/v1/content/admin/comments")]
[Tags("Content - Admin Comments")]
[Authorize(Roles = ContentRoles.Moderation)]
public sealed class AdminContentCommentsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AdminContentCommentsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
        => _cqrs = cqrs;

    [HttpGet]
    public async Task<AizenApiResponse<ContentCommentsResponse?>> Queue(
        [FromQuery] string contentId,
        [FromQuery] ContentCommentStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentCommentsResponse>(new GetAdminContentCommentsQuery
        {
            ContentId = contentId, Status = status, Page = page, PageSize = pageSize,
        }, ct);
        return SetResponse(result);
    }

    [HttpPost("{commentId}/moderate")]
    public async Task<AizenApiResponse<ContentCommentDto?>> Moderate(
        string commentId, [FromBody] ModerateContentCommentRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentCommentDto>(new ModerateContentCommentCommand
        {
            CommentId = commentId, Status = body.Status, Reason = body.Reason,
        }, ct);
        return SetResponse(result);
    }

    [HttpDelete("{commentId}")]
    public async Task<AizenApiResponse<ContentCommentDto?>> Delete(string commentId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentCommentDto>(
            new DeleteContentCommentCommand { CommentId = commentId }, ct);
        return SetResponse(result);
    }
}
