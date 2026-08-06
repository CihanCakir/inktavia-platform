using Aizen.Bff.AdminPanel.Application.Providers.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Providers.Query;

[DocumentationInfo("Get provider finance summary BFF query",
    "Composite finance summary for a provider. Composed from: " +
    "active subscription record (Payment module) + most recent payout records filtered by providerProfileId. " +
    "Both sources are fetched in parallel.")]
public sealed class GetProviderFinanceSummaryBffQuery : AizenQuery<ProviderFinanceSummaryBffResponse>
{
    public long ProviderProfileId { get; }
    public int  PayoutPageSize    { get; }

    public GetProviderFinanceSummaryBffQuery(long providerProfileId, int payoutPageSize = 10)
    {
        ProviderProfileId = providerProfileId;
        PayoutPageSize    = payoutPageSize;
    }
}
