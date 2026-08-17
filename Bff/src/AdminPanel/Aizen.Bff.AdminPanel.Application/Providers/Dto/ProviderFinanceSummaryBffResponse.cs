using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Bff.AdminPanel.Application.Providers.Dto;

[DocumentationInfo("Provider finance summary BFF response",
    "Composite finance summary for the provider operational detail panel. " +
    "Composed from: active subscription (Payment module) + paged payout records filtered by providerId. " +
    "Both sources are fetched in parallel. Each source can individually fail without crashing the whole response — " +
    "failures are recorded as AdminBffWarning entries.")]
public sealed class ProviderFinanceSummaryBffResponse
{
    public ActiveProviderSubscriptionResult? ActiveSubscription { get; set; }

    /// <summary>Most recent payout records for this provider (up to 10).</summary>
    public List<PaymentPayoutBffDto> RecentPayouts { get; set; } = new();

    public int    TotalPayoutCount  { get; set; }
    public decimal TotalPayoutAmount { get; set; }

    public List<AdminBffWarning> Warnings { get; set; } = new();
}
