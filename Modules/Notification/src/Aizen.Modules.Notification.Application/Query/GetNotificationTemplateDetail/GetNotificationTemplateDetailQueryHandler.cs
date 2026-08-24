using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Query.GetNotificationTemplateDetail;

public sealed class GetNotificationTemplateDetailQueryHandler
    : AizenQueryHandler<GetNotificationTemplateDetailQuery, NotificationTemplateDetailDto>
{
    private readonly INotificationTemplateRepository        _templateRepository;
    private readonly INotificationTemplateContentRepository _contentRepository;

    public GetNotificationTemplateDetailQueryHandler(
        INotificationTemplateRepository templateRepository,
        INotificationTemplateContentRepository contentRepository)
    {
        _templateRepository = templateRepository;
        _contentRepository  = contentRepository;
    }

    public override async Task<NotificationTemplateDetailDto> Handle(
        GetNotificationTemplateDetailQuery request, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByCodeAsync(request.Code, cancellationToken)
                       ?? throw new AizenBusinessException($"Template '{request.Code}' not found.");

        var contents = await _contentRepository.GetAllByTemplateIdAsync(template.Id, cancellationToken);

        // (channel, locale) başına bir hücre: yayın/taslak sürümleri + türetilmiş durum.
        var cells = contents
            .GroupBy(c => new { c.Channel, c.Locale })
            .Select(g =>
            {
                var published = g.Where(x => x.Status == TemplateContentStatus.Published)
                                 .OrderByDescending(x => x.Version).FirstOrDefault();
                var draft = g.Where(x => x.Status == TemplateContentStatus.Draft)
                             .OrderByDescending(x => x.Version).FirstOrDefault();

                // Türetilmiş durum: Published öncelikli; yoksa Draft; ikisi de yoksa (yalnız arşiv) Archived.
                var status = published is not null
                    ? TemplateContentStatus.Published
                    : draft is not null
                        ? TemplateContentStatus.Draft
                        : TemplateContentStatus.Archived;

                return new NotificationTemplateContentCellDto
                {
                    Channel          = g.Key.Channel,
                    Locale           = g.Key.Locale,
                    PublishedVersion = published?.Version,
                    DraftVersion     = draft?.Version,
                    Status           = status,
                };
            })
            .OrderBy(c => c.Channel).ThenBy(c => c.Locale)
            .ToList();

        return new NotificationTemplateDetailDto
        {
            Id           = template.Id,
            TemplateCode = template.TemplateCode,
            Name         = template.Name,
            Description  = template.Description,
            Type         = template.Type,
            Channel      = template.Channel,
            IsActive     = template.IsActive,
            Contents     = cells,
        };
    }
}
