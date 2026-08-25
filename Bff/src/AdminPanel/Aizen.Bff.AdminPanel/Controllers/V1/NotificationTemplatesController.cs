using Aizen.Bff.AdminPanel.Application.Notifications.Command;
using Aizen.Bff.AdminPanel.Application.Notifications.Query;
using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel/notification-templates")]
[Tags("Admin Panel - Notification Templates")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class NotificationTemplatesController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public NotificationTemplatesController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    /// <summary>GET api/v1/admin-panel/notification-templates — sayfalı liste {items,totalCount,page,pageSize}.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(NotificationTemplateListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationTemplateListResult>> GetPaged(
        [FromQuery] NotificationChannel? channel,
        [FromQuery] string? locale,
        [FromQuery] TemplateContentStatus? status,
        [FromQuery] bool? enabled,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetNotificationTemplatesPagedBffQuery
        {
            Channel = channel, Locale = locale, Status = status, Enabled = enabled,
            Search = search, Page = page, PageSize = pageSize,
        }, ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/notification-templates/{code} — mantıksal template + içerik matrisi.</summary>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(NotificationTemplateDetailDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationTemplateDetailDto>> GetByCode(string code, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<NotificationTemplateDetailDto>(
            new GetNotificationTemplateDetailBffQuery(code), ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/notification-templates/{code}/contents/{channel}/{locale} — hücrenin
    /// düzenlenebilir mevcut içeriği (Draft öncelikli, yoksa Published, yoksa boş). Editör körlemesine yazmasın diye.</summary>
    [HttpGet("{code}/contents/{channel}/{locale}")]
    [ProducesResponseType(typeof(NotificationTemplateContentEditResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationTemplateContentEditResult>> GetContentForEdit(
        string code, NotificationChannel channel, string locale, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<NotificationTemplateContentEditResult>(
            new GetNotificationTemplateContentForEditBffQuery { Code = code, Channel = channel, Locale = locale }, ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/notification-templates — mantıksal template oluştur.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(
        [FromBody] CreateNotificationTemplateRequest body, CancellationToken ct)
    {
        await _cqrs.ProcessAsync<AdminBffCommandResultDto>(new CreateNotificationTemplateBffCommand
        {
            TemplateCode  = body.TemplateCode,
            Name          = body.Name,
            Type          = body.Type,
            Channel       = body.Channel,
            TitleTemplate = body.TitleTemplate,
            BodyTemplate  = body.BodyTemplate,
        }, ct);
        return Ok();
    }

    /// <summary>PUT api/v1/admin-panel/notification-templates/{code} — meta güncelle (ad/açıklama/etkin).</summary>
    [HttpPut("{code}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateMeta(
        string code, [FromBody] UpdateNotificationTemplateMetaRequest body, CancellationToken ct)
    {
        await _cqrs.ProcessAsync<AdminBffCommandResultDto>(new UpdateNotificationTemplateMetaBffCommand
        {
            Code        = code,
            Name        = body.Name,
            Description  = body.Description,
            IsActive    = body.IsActive,
        }, ct);
        return NoContent();
    }

    /// <summary>PATCH api/v1/admin-panel/notification-templates/{code}/toggle — etkin/pasif.</summary>
    [HttpPatch("{code}/toggle")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Toggle(string code, CancellationToken ct)
    {
        await _cqrs.ProcessAsync<AdminBffCommandResultDto>(new ToggleNotificationTemplateBffCommand
        {
            Code = code,
        }, ct);
        return NoContent();
    }

    /// <summary>PUT .../{code}/contents/{channel}/{locale} — taslak kaydet (Published'a dokunmaz).</summary>
    [HttpPut("{code}/contents/{channel}/{locale}")]
    [ProducesResponseType(typeof(NotificationTemplateContentDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationTemplateContentDto>> SaveDraft(
        string code, NotificationChannel channel, string locale,
        [FromBody] SaveNotificationTemplateContentRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<NotificationTemplateContentDto>(new SaveTemplateContentDraftBffCommand
        {
            Code = code, Channel = channel, Locale = locale,
            SubjectTemplate = body.SubjectTemplate, HtmlTemplate = body.HtmlTemplate,
            TextTemplate = body.TextTemplate, LayoutCode = body.LayoutCode,
            TitleTemplate = body.TitleTemplate, BodyTemplate = body.BodyTemplate,
            DeepLinkTemplate = body.DeepLinkTemplate, SmsTextTemplate = body.SmsTextTemplate,
        }, ct);
        return SetResponse(result);
    }

    /// <summary>POST .../{code}/contents/{channel}/{locale}/publish — taslağı yayınla.</summary>
    [HttpPost("{code}/contents/{channel}/{locale}/publish")]
    [ProducesResponseType(typeof(NotificationTemplateContentDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationTemplateContentDto>> Publish(
        string code, NotificationChannel channel, string locale, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<NotificationTemplateContentDto>(new PublishTemplateContentBffCommand
        {
            Code = code, Channel = channel, Locale = locale,
        }, ct);
        return SetResponse(result);
    }

    /// <summary>GET .../{code}/versions?channel&amp;locale — sürüm listesi.</summary>
    [HttpGet("{code}/versions")]
    [ProducesResponseType(typeof(List<NotificationTemplateVersionDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<NotificationTemplateVersionDto>>> GetVersions(
        string code, [FromQuery] NotificationChannel channel, [FromQuery] string locale, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<List<NotificationTemplateVersionDto>>(
            new GetTemplateContentVersionsBffQuery { Code = code, Channel = channel, Locale = locale }, ct);
        return SetResponse(result);
    }

    /// <summary>
    /// POST .../{code}/preview — ÜRETİMLE aynı renderer ile önizleme. Eksik placeholder → 400 + eksik anahtar listesi
    /// (modül S2S Refit gövde-tipli dönüşte non-2xx'te exception attığı için sonucu gövdede taşır; 400'ü BURADA üretiriz).
    /// </summary>
    [HttpPost("{code}/preview")]
    [ProducesResponseType(typeof(NotificationTemplatePreviewResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(NotificationTemplatePreviewResultDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AizenApiResponse<NotificationTemplatePreviewResultDto>>> Preview(
        string code, [FromBody] NotificationTemplatePreviewRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<NotificationTemplatePreviewResultDto>(new PreviewTemplateContentBffQuery
        {
            Code = code, Channel = body.Channel, Locale = body.Locale, Variables = body.Variables,
        }, ct);

        var envelope = SetResponse(result);
        // Eksik placeholder (ya da yayında içerik yok) → 400; render başarılıysa 200.
        return result is { Rendered: false }
            ? BadRequest(envelope)
            : Ok(envelope);
    }

    /// <summary>GET .../{code}/variables — template tipine göre izin verilen {{placeholder}} kataloğu.</summary>
    [HttpGet("{code}/variables")]
    [ProducesResponseType(typeof(NotificationTemplateVariablesDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationTemplateVariablesDto>> GetVariables(string code, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<NotificationTemplateVariablesDto>(
            new GetTemplateVariablesBffQuery(code), ct);
        return SetResponse(result);
    }
}

// ─── Request models (BFF-side; Create still mirrors the module create request) ───────────
public sealed class CreateNotificationTemplateRequest
{
    public string              TemplateCode  { get; set; } = default!;
    public string              Name          { get; set; } = default!;
    public NotificationType    Type          { get; set; }
    public NotificationChannel Channel       { get; set; }
    public string              TitleTemplate { get; set; } = default!;
    public string              BodyTemplate  { get; set; } = default!;
}
