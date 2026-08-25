using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Application.Mapping;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Query.GetNotificationCampaignsPaged;

public sealed class GetNotificationCampaignsPagedQueryHandler
    : AizenQueryHandler<GetNotificationCampaignsPagedQuery, NotificationCampaignListResult>
{
    private readonly INotificationCampaignRepository _repository;

    public GetNotificationCampaignsPagedQueryHandler(INotificationCampaignRepository repository)
        => _repository = repository;

    public override async Task<NotificationCampaignListResult> Handle(
        GetNotificationCampaignsPagedQuery request, CancellationToken cancellationToken)
    {
        var page     = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;
        var skip     = (page - 1) * pageSize;

        var (items, total) = await _repository.GetPagedAsync(skip, pageSize, cancellationToken);

        return new NotificationCampaignListResult
        {
            Items      = items.Select(c => c.ToListItemDto()).ToList(),
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize,
        };
    }
}
