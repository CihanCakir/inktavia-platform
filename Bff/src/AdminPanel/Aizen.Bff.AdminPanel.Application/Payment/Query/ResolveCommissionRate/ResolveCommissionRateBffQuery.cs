using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.ResolveCommissionRate;

public sealed class ResolveCommissionRateBffQuery : AizenQuery<ResolveCommissionRateBffResponse>
{
    public long?   ProviderProfileId { get; init; }
    public long?   ProviderPlanId    { get; init; }
    public string? CategoryCode      { get; init; }
}

public sealed class ResolveCommissionRateBffResponse
{
    public CommissionRateBffResult Rate { get; init; } = default!;
}
