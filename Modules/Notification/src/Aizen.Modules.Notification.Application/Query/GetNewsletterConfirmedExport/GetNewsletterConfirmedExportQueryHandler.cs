using System.Collections.Generic;
using System.Linq;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Application.Mapping;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Query.GetNewsletterConfirmedExport;

public sealed class GetNewsletterConfirmedExportQueryHandler
    : AizenQueryHandler<GetNewsletterConfirmedExportQuery, List<NewsletterSubscriberListItemDto>>
{
    private readonly INewsletterSubscriberRepository _repository;

    public GetNewsletterConfirmedExportQueryHandler(INewsletterSubscriberRepository repository)
        => _repository = repository;

    public override async Task<List<NewsletterSubscriberListItemDto>> Handle(
        GetNewsletterConfirmedExportQuery request, CancellationToken cancellationToken)
    {
        var rows = await _repository.GetConfirmedForExportAsync(cancellationToken);
        return rows.Select(s => s.ToListItemDto()).ToList();
    }
}
