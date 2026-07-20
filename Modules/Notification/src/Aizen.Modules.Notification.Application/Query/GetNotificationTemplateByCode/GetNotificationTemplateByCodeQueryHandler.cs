using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Query.GetNotificationTemplateByCode;

public sealed class GetNotificationTemplateByCodeQueryHandler
    : AizenQueryHandler<GetNotificationTemplateByCodeQuery, NotificationTemplateDto?>
{
    private readonly INotificationTemplateRepository _repository;

    public GetNotificationTemplateByCodeQueryHandler(INotificationTemplateRepository repository)
        => _repository = repository;

    public override async Task<NotificationTemplateDto?> Handle(
        GetNotificationTemplateByCodeQuery request, CancellationToken cancellationToken)
    {
        var t = await _repository.GetByCodeAsync(request.Code, cancellationToken);
        if (t is null) return null;

        return new NotificationTemplateDto
        {
            Id            = t.Id,
            TemplateCode  = t.TemplateCode,
            Name          = t.Name,
            Type          = t.Type,
            Channel       = t.Channel,
            TitleTemplate = t.TitleTemplate,
            BodyTemplate  = t.BodyTemplate,
            IsActive      = t.IsActive,
        };
    }
}
