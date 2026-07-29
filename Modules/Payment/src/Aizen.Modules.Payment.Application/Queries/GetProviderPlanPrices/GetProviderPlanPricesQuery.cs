using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Application.Queries.ResolveProviderPlanPrice;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderPlanPrices;

/// <summary>Admin: lists all price versions for a plan (ordered by billing period then EffectiveFrom).</summary>
public sealed class GetProviderPlanPricesQuery : AizenQuery<List<ProviderPlanPriceResult>>
{
    public required long ProviderPlanId { get; init; }
}
