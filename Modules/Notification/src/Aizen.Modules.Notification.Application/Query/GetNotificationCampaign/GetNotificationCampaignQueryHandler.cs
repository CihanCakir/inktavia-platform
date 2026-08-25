using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Application.Mapping;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Query.GetNotificationCampaign;

public sealed class GetNotificationCampaignQueryHandler
    : AizenQueryHandler<GetNotificationCampaignQuery, NotificationCampaignDto>
{
    private readonly INotificationCampaignRepository _repository;

    public GetNotificationCampaignQueryHandler(INotificationCampaignRepository repository)
        => _repository = repository;

    public override async Task<NotificationCampaignDto?> Handle(
        GetNotificationCampaignQuery request, CancellationToken cancellationToken)
    {
        var campaign = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return campaign?.ToDto();   // yoksa null → controller boş gövde döner.
    }
}
