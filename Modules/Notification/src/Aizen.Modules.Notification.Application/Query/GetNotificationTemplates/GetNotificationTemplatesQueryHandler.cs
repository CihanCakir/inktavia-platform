using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Query.GetNotificationTemplates;

public sealed class GetNotificationTemplatesQueryHandler
    : AizenQueryHandler<GetNotificationTemplatesQuery, List<NotificationTemplateDto>>
{
    private readonly INotificationTemplateRepository _repository;

    public GetNotificationTemplatesQueryHandler(INotificationTemplateRepository repository)
        => _repository = repository;

    public override async Task<List<NotificationTemplateDto>> Handle(
        GetNotificationTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await _repository.GetAllAsync(cancellationToken);
        return templates.Select(t => new NotificationTemplateDto
        {
            Id            = t.Id,
            TemplateCode  = t.TemplateCode,
            Name          = t.Name,
            Type          = t.Type,
            Channel       = t.Channel,
            TitleTemplate = t.TitleTemplate,
            BodyTemplate  = t.BodyTemplate,
            IsActive      = t.IsActive,
        }).ToList();
    }
}
