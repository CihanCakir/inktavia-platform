using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Queries.GetAdminSubscriptionList;

public sealed class GetAdminSubscriptionListQuery : AizenQuery<AdminSubscriptionListResult>
{
    /// <summary>Filter by audience: "Provider" | "Participant" | null (all)</summary>
    public string? Audience { get; init; }

    /// <summary>Filter by status: "Active" | "PastDue" | "Cancelled" | "Expired" | null (all)</summary>
    public string? Status { get; init; }

    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed record AdminSubscriptionListItemDto(
    long     Id,
    string   Audience,        // "Provider" | "Participant"
    long     ProfileId,
    long     PlanId,
    string   PlanCode,
    string   PlanName,
    string   Status,          // SubscriptionStatus.ToString()
    decimal  PaidAmount,
    string   CurrencyCode,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    bool     AutoRenew,
    DateTime CreateDate
);

public sealed record AdminSubscriptionListResult(
    List<AdminSubscriptionListItemDto> Items,
    int Total,
    int Page,
    int PageSize
);
