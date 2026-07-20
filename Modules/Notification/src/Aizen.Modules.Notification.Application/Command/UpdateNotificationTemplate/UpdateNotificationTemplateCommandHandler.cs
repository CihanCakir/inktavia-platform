using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Command.UpdateNotificationTemplate;

public sealed class UpdateNotificationTemplateCommandHandler
    : AizenCommandHandler<UpdateNotificationTemplateCommand, NotificationTemplateMutationResponse>
{
    private readonly INotificationTemplateRepository _repository;

    public UpdateNotificationTemplateCommandHandler(INotificationTemplateRepository repository)
        => _repository = repository;

    public override async Task<NotificationTemplateMutationResponse?> Handle(
        UpdateNotificationTemplateCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByCodeAsync(request.Code, cancellationToken);
        if (entity is null)
            throw new AizenBusinessException($"Template '{request.Code}' not found.");

        entity.Update(request.Name, request.TitleTemplate, request.BodyTemplate);
        await _repository.UpdateAsync(entity, cancellationToken);

        return new NotificationTemplateMutationResponse
        {
            TemplateCode = entity.TemplateCode,
            Success      = true,
        };
    }
}
