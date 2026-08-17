using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Command.CreateNotificationTemplate;

public sealed class CreateNotificationTemplateCommandHandler
    : AizenCommandHandler<CreateNotificationTemplateCommand, NotificationTemplateMutationResponse>
{
    private readonly INotificationTemplateRepository _repository;

    public CreateNotificationTemplateCommandHandler(INotificationTemplateRepository repository)
        => _repository = repository;

    public override async Task<NotificationTemplateMutationResponse?> Handle(
        CreateNotificationTemplateCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByCodeAsync(request.TemplateCode, cancellationToken);
        if (existing is not null)
            throw new AizenBusinessException($"Template '{request.TemplateCode}' already exists.");

        var entity = NotificationTemplateEntity.Create(
            request.TemplateCode, request.Name, request.Type, request.Channel,
            request.TitleTemplate, request.BodyTemplate);

        await _repository.AddAsync(entity, cancellationToken);

        return new NotificationTemplateMutationResponse
        {
            TemplateCode = entity.TemplateCode,
            Success      = true,
        };
    }
}
