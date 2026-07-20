using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Application.Command.CreateNotificationTemplate;
using Aizen.Modules.Notification.Application.Command.ToggleNotificationTemplate;
using Aizen.Modules.Notification.Application.Command.UpdateNotificationTemplate;
using Aizen.Modules.Notification.Application.Query.GetNotificationTemplateByCode;
using Aizen.Modules.Notification.Application.Query.GetNotificationTemplates;
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
}
