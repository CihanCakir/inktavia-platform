using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Application.Command.CreateNotificationTemplate;
using Aizen.Modules.Notification.Application.Command.PublishTemplateContent;
using Aizen.Modules.Notification.Application.Command.SaveTemplateContentDraft;
using Aizen.Modules.Notification.Application.Command.ToggleNotificationTemplate;
using Aizen.Modules.Notification.Application.Command.UpdateNotificationTemplate;
using Aizen.Modules.Notification.Application.Command.UpdateNotificationTemplateMeta;
using Aizen.Modules.Notification.Application.Query.GetNotificationTemplateByCode;
using Aizen.Modules.Notification.Application.Query.GetNotificationTemplateDetail;
using Aizen.Modules.Notification.Application.Query.GetNotificationTemplates;
using Aizen.Modules.Notification.Application.Query.GetNotificationTemplatesPaged;
using Aizen.Modules.Notification.Application.Query.GetTemplateContentForEdit;
using Aizen.Modules.Notification.Application.Query.GetTemplateContentVersions;
using Aizen.Modules.Notification.Application.Query.GetTemplateVariables;
using Aizen.Modules.Notification.Application.Query.PreviewTemplateContent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Notification.Controllers;

[ApiController]
[Route("api/v1/notification/admin/notification-templates")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class NotificationTemplatesController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public NotificationTemplatesController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpGet]
    [ProducesResponseType(typeof(AizenApiResponse<List<NotificationTemplateDto>>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<NotificationTemplateDto>?>> GetAll(CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync<List<NotificationTemplateDto>>(
               new GetNotificationTemplatesQuery(), ct));

    [HttpGet("{code}")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationTemplateDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationTemplateDto?>> GetByCode(string code, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync<NotificationTemplateDto?>(
               new GetNotificationTemplateByCodeQuery { Code = code }, ct));

    [HttpPost]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationTemplateMutationResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationTemplateMutationResponse?>> Create(
        [FromBody] CreateNotificationTemplateRequest body, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync<NotificationTemplateMutationResponse>(
               new CreateNotificationTemplateCommand
               {
                   TemplateCode  = body.TemplateCode,
                   Name          = body.Name,
                   Type          = body.Type,
                   Channel       = body.Channel,
                   TitleTemplate = body.TitleTemplate,
                   BodyTemplate  = body.BodyTemplate,
               }, ct));

    [HttpPut("{code}")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationTemplateMutationResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationTemplateMutationResponse?>> Update(
        string code, [FromBody] UpdateNotificationTemplateRequest body, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync<NotificationTemplateMutationResponse>(
               new UpdateNotificationTemplateCommand
               {
                   Code          = code,
                   Name          = body.Name,
                   TitleTemplate = body.TitleTemplate,
                   BodyTemplate  = body.BodyTemplate,
               }, ct));

    [HttpPatch("{code}/toggle")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationTemplateMutationResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationTemplateMutationResponse?>> Toggle(string code, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync<NotificationTemplateMutationResponse>(
               new ToggleNotificationTemplateCommand { Code = code }, ct));

    // ─── Phase-2 channel×locale×version admin endpoints ──────────────────────────────────────────

    /// <summary>Sayfalı liste: channel/locale/status/enabled/search filtreleriyle {items,totalCount,page,pageSize}.</summary>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationTemplateListResult>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationTemplateListResult?>> GetPaged(
        [FromQuery] NotificationChannel? channel,
        [FromQuery] string? locale,
        [FromQuery] TemplateContentStatus? status,
        [FromQuery] bool? enabled,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<NotificationTemplateListResult>(
               new GetNotificationTemplatesPagedQuery
               {
                   Channel = channel, Locale = locale, Status = status, Enabled = enabled,
                   Search = search, Page = page, PageSize = pageSize,
               }, ct));

    /// <summary>Mantıksal template + içerik matrisi (channel→locale hücreleri).</summary>
    [HttpGet("{code}/detail")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationTemplateDetailDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationTemplateDetailDto?>> GetDetail(string code, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync<NotificationTemplateDetailDto>(
               new GetNotificationTemplateDetailQuery { Code = code }, ct));

    /// <summary>Meta güncelle: ad + açıklama + etkinleştirme.</summary>
    [HttpPut("{code}/meta")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationTemplateMutationResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationTemplateMutationResponse?>> UpdateMeta(
        string code, [FromBody] UpdateNotificationTemplateMetaRequest body, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync<NotificationTemplateMutationResponse>(
               new UpdateNotificationTemplateMetaCommand
               {
                   Code = code, Name = body.Name, Description = body.Description, IsActive = body.IsActive,
               }, ct));

    /// <summary>(channel, locale) hücresinin DÜZENLENEBİLİR mevcut içeriği: Draft öncelikli, yoksa Published, yoksa boş.</summary>
    [HttpGet("{code}/contents/{channel}/{locale}")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationTemplateContentEditResult>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationTemplateContentEditResult?>> GetContentForEdit(
        string code, NotificationChannel channel, string locale, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync<NotificationTemplateContentEditResult>(
               new GetTemplateContentForEditQuery { Code = code, Channel = channel, Locale = locale }, ct));

    /// <summary>Taslak kaydet: mevcut Draft'ı günceller ya da yeni Draft sürüm oluşturur. Published'a dokunmaz.</summary>
    [HttpPut("{code}/contents/{channel}/{locale}")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationTemplateContentDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationTemplateContentDto?>> SaveDraft(
        string code, NotificationChannel channel, string locale,
        [FromBody] SaveNotificationTemplateContentRequest body, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync<NotificationTemplateContentDto>(
               new SaveTemplateContentDraftCommand
               {
                   Code = code, Channel = channel, Locale = locale,
                   SubjectTemplate = body.SubjectTemplate, HtmlTemplate = body.HtmlTemplate,
                   TextTemplate = body.TextTemplate, LayoutCode = body.LayoutCode,
                   TitleTemplate = body.TitleTemplate, BodyTemplate = body.BodyTemplate,
                   DeepLinkTemplate = body.DeepLinkTemplate, SmsTextTemplate = body.SmsTextTemplate,
               }, ct));

    /// <summary>Yayınla: Draft → Published; önceki Published → Archived (tek transaction).</summary>
    [HttpPost("{code}/contents/{channel}/{locale}/publish")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationTemplateContentDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationTemplateContentDto?>> Publish(
        string code, NotificationChannel channel, string locale, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync<NotificationTemplateContentDto>(
               new PublishTemplateContentCommand { Code = code, Channel = channel, Locale = locale }, ct));

    /// <summary>(channel, locale) için sürüm listesi (en yeni önce).</summary>
    [HttpGet("{code}/versions")]
    [ProducesResponseType(typeof(AizenApiResponse<List<NotificationTemplateVersionDto>>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<NotificationTemplateVersionDto>?>> GetVersions(
        string code, [FromQuery] NotificationChannel channel, [FromQuery] string locale, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync<List<NotificationTemplateVersionDto>>(
               new GetTemplateContentVersionsQuery { Code = code, Channel = channel, Locale = locale }, ct));

    /// <summary>Preview: ÜRETİMLE aynı renderer. Eksik placeholder → gövdede MissingKeys (400'ü uç/BFF üretir).</summary>
    [HttpPost("{code}/preview")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationTemplatePreviewResultDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationTemplatePreviewResultDto?>> Preview(
        string code, [FromBody] NotificationTemplatePreviewRequest body, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync<NotificationTemplatePreviewResultDto>(
               new PreviewTemplateContentQuery
               {
                   Code = code, Channel = body.Channel, Locale = body.Locale, Variables = body.Variables,
               }, ct));

    /// <summary>Değişken kataloğu: template tipine göre izin verilen {{placeholder}} adları.</summary>
    [HttpGet("{code}/variables")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationTemplateVariablesDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationTemplateVariablesDto?>> GetVariables(string code, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync<NotificationTemplateVariablesDto>(
               new GetTemplateVariablesQuery { Code = code }, ct));
}
