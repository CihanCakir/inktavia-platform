using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Query.GetNotificationTemplatesPaged;

public sealed class GetNotificationTemplatesPagedQueryHandler
    : AizenQueryHandler<GetNotificationTemplatesPagedQuery, NotificationTemplateListResult>
{
    private readonly INotificationTemplateRepository _repository;

    public GetNotificationTemplatesPagedQueryHandler(INotificationTemplateRepository repository)
        => _repository = repository;

    public override async Task<NotificationTemplateListResult> Handle(
        GetNotificationTemplatesPagedQuery request, CancellationToken cancellationToken)
    {
        // Sayfa/boyut güvenli aralığa çekilir (1 tabanlı sayfa, makul üst sınır).
        var page     = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;
        var skip     = (page - 1) * pageSize;

        var (items, total) = await _repository.GetPagedAsync(
            request.Channel, request.Locale, request.Status, request.Enabled, request.Search,
            skip, pageSize, cancellationToken);

        return new NotificationTemplateListResult
        {
            Items = items.Select(t => new NotificationTemplateListItemDto
            {
                Id           = t.Id,
                TemplateCode = t.TemplateCode,
                Name         = t.Name,
                Description  = t.Description,
                Type         = t.Type,
                Channel      = t.Channel,
                IsActive     = t.IsActive,
            }).ToList(),
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize,
        };
    }
}
