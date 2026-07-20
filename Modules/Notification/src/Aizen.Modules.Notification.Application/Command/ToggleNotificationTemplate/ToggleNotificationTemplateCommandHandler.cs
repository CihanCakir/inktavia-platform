using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Command.ToggleNotificationTemplate;

public sealed class ToggleNotificationTemplateCommandHandler
    : AizenCommandHandler<ToggleNotificationTemplateCommand, NotificationTemplateMutationResponse>
{
    private readonly INotificationTemplateRepository _repository;

    public ToggleNotificationTemplateCommandHandler(INotificationTemplateRepository repository)
        => _repository = repository;

    public override async Task<NotificationTemplateMutationResponse?> Handle(
        ToggleNotificationTemplateCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByCodeAsync(request.Code, cancellationToken);
        if (entity is null)
            throw new AizenBusinessException($"Template '{request.Code}' not found.");

        entity.SetActive(!entity.IsActive);
        await _repository.UpdateAsync(entity, cancellationToken);

        return new NotificationTemplateMutationResponse
        {
            TemplateCode = entity.TemplateCode,
            Success      = true,
        };
    }
}
