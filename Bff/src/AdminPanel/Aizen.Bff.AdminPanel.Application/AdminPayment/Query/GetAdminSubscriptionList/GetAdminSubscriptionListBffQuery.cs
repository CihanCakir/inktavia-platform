using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetAdminSubscriptionList;

public sealed class GetAdminSubscriptionListBffQuery : AizenQuery<GetAdminSubscriptionListBffResponse>
{
    /// <summary>Filter by audience: "Provider" | "Participant" | null (all)</summary>
    public string? Audience { get; init; }

    /// <summary>Filter by status: "Active" | "PastDue" | "Cancelled" | "Expired" | null (all)</summary>
    public string? Status { get; init; }

    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed class GetAdminSubscriptionListBffResponse
{
    public AdminSubscriptionListBffResult Result { get; init; } = default!;
}
