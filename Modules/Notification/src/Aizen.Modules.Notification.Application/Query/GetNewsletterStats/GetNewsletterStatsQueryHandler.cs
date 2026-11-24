using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Query.GetNewsletterStats;

public sealed class GetNewsletterStatsQueryHandler
    : AizenQueryHandler<GetNewsletterStatsQuery, NewsletterStatsDto>
{
    private readonly INewsletterSubscriberRepository _repository;

    public GetNewsletterStatsQueryHandler(INewsletterSubscriberRepository repository)
        => _repository = repository;

    public override async Task<NewsletterStatsDto> Handle(
        GetNewsletterStatsQuery request, CancellationToken cancellationToken)
    {
        var counts = await _repository.GetStatusCountsAsync(cancellationToken);
        var since  = DateTimeOffset.UtcNow.AddDays(-30);
        var newLast30 = await _repository.CountCreatedSinceAsync(since, cancellationToken);

        int Count(NewsletterSubscriberStatus s) => counts.TryGetValue(s, out var c) ? c : 0;

        return new NewsletterStatsDto
        {
            Pending         = Count(NewsletterSubscriberStatus.Pending),
            Confirmed       = Count(NewsletterSubscriberStatus.Confirmed),
            Unsubscribed    = Count(NewsletterSubscriberStatus.Unsubscribed),
            Bounced         = Count(NewsletterSubscriberStatus.Bounced),
            Suppressed      = Count(NewsletterSubscriberStatus.Suppressed),
            Total           = counts.Values.Sum(),
            NewInLast30Days = newLast30,
        };
    }
}
