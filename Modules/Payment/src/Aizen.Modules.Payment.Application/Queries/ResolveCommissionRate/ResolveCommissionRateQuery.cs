using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Queries.ResolveCommissionRate;

public sealed class ResolveCommissionRateQuery : AizenQuery<CommissionRateResult>
{
    public long?   ProviderProfileId { get; init; }
    public long?   ProviderPlanId    { get; init; }
    public string? CategoryCode      { get; init; }
}

public sealed record CommissionRateResult(decimal Rate, string ResolvedFrom);
