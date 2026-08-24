using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Application.Mapping;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Query.GetTemplateContentVersions;

public sealed class GetTemplateContentVersionsQueryHandler
    : AizenQueryHandler<GetTemplateContentVersionsQuery, List<NotificationTemplateVersionDto>>
{
    private readonly INotificationTemplateRepository        _templateRepository;
    private readonly INotificationTemplateContentRepository _contentRepository;

    public GetTemplateContentVersionsQueryHandler(
        INotificationTemplateRepository templateRepository,
        INotificationTemplateContentRepository contentRepository)
    {
        _templateRepository = templateRepository;
        _contentRepository  = contentRepository;
    }

    public override async Task<List<NotificationTemplateVersionDto>> Handle(
        GetTemplateContentVersionsQuery request, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByCodeAsync(request.Code, cancellationToken)
                       ?? throw new AizenBusinessException($"Template '{request.Code}' not found.");

        var versions = await _contentRepository.GetVersionsAsync(
            template.Id, request.Channel, request.Locale, cancellationToken);

        return versions.Select(v => v.ToVersionDto()).ToList();
    }
}
