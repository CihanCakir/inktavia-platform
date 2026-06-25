using Aizen.Bff.AdminPanel.Application.AdminNotificationTemplates.Command;
using Aizen.Bff.AdminPanel.Application.AdminNotificationTemplates.Query;
using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel/notification-templates")]
[Tags("Admin Panel - Notification Templates")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class AdminNotificationTemplatesController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AdminNotificationTemplatesController(
        IHttpContextAccessor httpContextAccessor,
        IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    private string GetUserToken()
    {
        var raw = HttpContext.Request.Headers.Authorization.ToString();
        return raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? raw["Bearer ".Length..]
            : raw;
    }

    /// <summary>GET api/v1/admin-panel/notification-templates</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<NotificationTemplateDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<NotificationTemplateDto>>> GetAll(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<List<NotificationTemplateDto>>(
            new GetAdminNotificationTemplatesQuery(GetUserToken()), ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/notification-templates/{code}</summary>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(NotificationTemplateDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationTemplateDto>> GetByCode(
        string code, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<NotificationTemplateDto>(
            new GetAdminNotificationTemplateByCodeQuery(code, GetUserToken()), ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/notification-templates</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(
        [FromBody] CreateNotificationTemplateRequest body, CancellationToken ct)
    {
        await _cqrs.ProcessAsync<AdminBffCommandResultDto>(new CreateNotificationTemplateCommand
        {
            TemplateCode  = body.TemplateCode,
            Name          = body.Name,
            Type          = body.Type,
            Channel       = body.Channel,
            TitleTemplate = body.TitleTemplate,
            BodyTemplate  = body.BodyTemplate,
            UserToken     = GetUserToken()
        }, ct);
        return Ok();
    }

    /// <summary>PUT api/v1/admin-panel/notification-templates/{code}</summary>
    [HttpPut("{code}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        string code,
        [FromBody] UpdateNotificationTemplateRequest body,
        CancellationToken ct)
    {
        await _cqrs.ProcessAsync<AdminBffCommandResultDto>(new UpdateNotificationTemplateCommand
        {
            Code          = code,
            Name          = body.Name,
            TitleTemplate = body.TitleTemplate,
            BodyTemplate  = body.BodyTemplate,
            UserToken     = GetUserToken()
        }, ct);
        return NoContent();
    }

    /// <summary>PATCH api/v1/admin-panel/notification-templates/{code}/toggle</summary>
    [HttpPatch("{code}/toggle")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Toggle(string code, CancellationToken ct)
    {
        await _cqrs.ProcessAsync<AdminBffCommandResultDto>(new ToggleNotificationTemplateCommand
        {
            Code      = code,
            UserToken = GetUserToken()
        }, ct);
        return NoContent();
    }
}

// ─── Request models (BFF-side, mirror Notification module requests) ───────────

public sealed class CreateNotificationTemplateRequest
{
    public string              TemplateCode  { get; set; } = default!;
    public string              Name          { get; set; } = default!;
    public NotificationType    Type          { get; set; }
    public NotificationChannel Channel       { get; set; }
    public string              TitleTemplate { get; set; } = default!;
    public string              BodyTemplate  { get; set; } = default!;
}

public sealed class UpdateNotificationTemplateRequest
{
    public string Name          { get; set; } = default!;
    public string TitleTemplate { get; set; } = default!;
    public string BodyTemplate  { get; set; } = default!;
}
