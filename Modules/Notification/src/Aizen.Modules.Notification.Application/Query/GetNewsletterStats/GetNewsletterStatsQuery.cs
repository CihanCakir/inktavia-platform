using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Modules.Notification.Application.Query.GetNewsletterStats;

/// <summary>P1 — newsletter dashboard counters (per status + new-in-last-30-days).</summary>
public sealed class GetNewsletterStatsQuery : AizenQuery<NewsletterStatsDto>
{
}
