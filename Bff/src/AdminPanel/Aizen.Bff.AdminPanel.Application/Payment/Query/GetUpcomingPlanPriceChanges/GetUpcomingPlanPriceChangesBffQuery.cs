using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetUpcomingPlanPriceChanges;


// ─── Upcoming price changes ──────────────────────────────────────────────────
public sealed class GetUpcomingPlanPriceChangesBffQuery : AizenQuery<GetUpcomingPlanPriceChangesBffResponse>
{
    public int WithinDays { get; init; } = 14;
}
public sealed class GetUpcomingPlanPriceChangesBffResponse { public List<UpcomingPriceChangeBffItem> Items { get; init; } = new(); }
