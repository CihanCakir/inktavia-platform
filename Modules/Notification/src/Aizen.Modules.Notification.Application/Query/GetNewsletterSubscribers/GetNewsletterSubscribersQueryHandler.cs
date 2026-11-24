using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Application.Mapping;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Query.GetNewsletterSubscribers;

public sealed class GetNewsletterSubscribersQueryHandler
    : AizenQueryHandler<GetNewsletterSubscribersQuery, NewsletterSubscriberListResult>
{
    private readonly INewsletterSubscriberRepository _repository;

    public GetNewsletterSubscribersQueryHandler(INewsletterSubscriberRepository repository)
        => _repository = repository;

    public override async Task<NewsletterSubscriberListResult> Handle(
        GetNewsletterSubscribersQuery request, CancellationToken cancellationToken)
    {
        var page     = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;
        var skip     = (page - 1) * pageSize;

        var (items, total) = await _repository.GetPagedAsync(
            request.Status, request.Lang, request.Search, request.From, request.To, skip, pageSize, cancellationToken);

        return new NewsletterSubscriberListResult
        {
            Items      = items.Select(s => s.ToListItemDto()).ToList(),
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize,
        };
    }
}
