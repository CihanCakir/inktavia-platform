using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Query.GetNotificationTemplates;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Notification.Controllers;

[ApiController]
[Route("api/v1/notification/admin/notification-templates")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class NotificationTemplatesController : ControllerBase
{
    private readonly ISender                         _sender;
    private readonly INotificationTemplateRepository _repository;

    public NotificationTemplatesController(
        ISender sender,
        INotificationTemplateRepository repository)
    {
        _sender     = sender;
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _sender.Send(new GetNotificationTemplatesQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(string code, CancellationToken ct)
    {
        var template = await _repository.GetByCodeAsync(code, ct);
        return template is null ? NotFound() : Ok(template);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateNotificationTemplateRequest body,
        CancellationToken ct)
    {
        var existing = await _repository.GetByCodeAsync(body.TemplateCode, ct);
        if (existing is not null)
            return Conflict($"Template '{body.TemplateCode}' already exists.");

        var entity = NotificationTemplateEntity.Create(
            body.TemplateCode, body.Name, body.Type, body.Channel,
            body.TitleTemplate, body.BodyTemplate);

        await _repository.AddAsync(entity, ct);
        return CreatedAtAction(nameof(GetByCode), new { code = entity.TemplateCode }, entity);
    }

    [HttpPut("{code}")]
    public async Task<IActionResult> Update(
        string code,
        [FromBody] UpdateNotificationTemplateRequest body,
        CancellationToken ct)
    {
        var entity = await _repository.GetByCodeAsync(code, ct);
        if (entity is null) return NotFound();

        entity.Update(body.Name, body.TitleTemplate, body.BodyTemplate);
        await _repository.UpdateAsync(entity, ct);
        return NoContent();
    }

    [HttpPatch("{code}/toggle")]
    public async Task<IActionResult> Toggle(string code, CancellationToken ct)
    {
        var entity = await _repository.GetByCodeAsync(code, ct);
        if (entity is null) return NotFound();

        entity.SetActive(!entity.IsActive);
        await _repository.UpdateAsync(entity, ct);
        return NoContent();
    }
}

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
