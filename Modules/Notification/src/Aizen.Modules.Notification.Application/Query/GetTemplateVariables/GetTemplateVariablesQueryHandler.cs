using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Query.GetTemplateVariables;

public sealed class GetTemplateVariablesQueryHandler
    : AizenQueryHandler<GetTemplateVariablesQuery, NotificationTemplateVariablesDto>
{
    private readonly INotificationTemplateRepository _templateRepository;

    public GetTemplateVariablesQueryHandler(INotificationTemplateRepository templateRepository)
        => _templateRepository = templateRepository;

    public override async Task<NotificationTemplateVariablesDto> Handle(
        GetTemplateVariablesQuery request, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByCodeAsync(request.Code, cancellationToken)
                       ?? throw new AizenBusinessException($"Template '{request.Code}' not found.");

        // İzin verilen placeholder'lar template'in NotificationType'ından kataloğa göre türetilir.
        var placeholders = NotificationVariablesCatalog.GetAllowedPlaceholders(template.Type);

        return new NotificationTemplateVariablesDto
        {
            TemplateCode = template.TemplateCode,
            Type         = template.Type,
            Placeholders = placeholders,
        };
    }
}
