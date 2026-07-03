using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Queries.GetSubscriptionMrrTrend;

public sealed class GetSubscriptionMrrTrendQuery : AizenQuery<SubscriptionMrrTrendResult>
{
    /// <summary>Number of months to include in the trend (default 6).</summary>
    public int Months { get; init; } = 6;
}

public sealed record MrrMonthDto(
    int     Year,
    int     Month,
    string  Label,    // e.g. "Oca", "Şub", ...
    decimal MrrTotal
);

public sealed record SubscriptionMrrTrendResult(
    List<MrrMonthDto> Months
);
