using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Command.UpdateNotificationTemplateMeta;

public sealed class UpdateNotificationTemplateMetaCommandHandler
    : AizenCommandHandler<UpdateNotificationTemplateMetaCommand, NotificationTemplateMutationResponse>
{
    private readonly INotificationTemplateRepository _repository;

    public UpdateNotificationTemplateMetaCommandHandler(INotificationTemplateRepository repository)
        => _repository = repository;

    public override async Task<NotificationTemplateMutationResponse?> Handle(
        UpdateNotificationTemplateMetaCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByCodeAsync(request.Code, cancellationToken)
                     ?? throw new AizenBusinessException($"Template '{request.Code}' not found.");

        entity.UpdateMeta(request.Name, request.Description, request.IsActive);
        await _repository.UpdateAsync(entity, cancellationToken);

        return new NotificationTemplateMutationResponse
        {
            TemplateCode = entity.TemplateCode,
            Success      = true,
        };
    }
}
