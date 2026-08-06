using Aizen.Bff.AdminPanel.Application.Providers.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.Providers.Query;

[DocumentationInfo("Get provider finance summary BFF query handler",
    "Fetches provider subscription and payout data in parallel. " +
    "Each source can individually fail without crashing the whole response — failures are recorded as warnings. " +
    "Read-only. No payment state is changed.")]
public sealed class GetProviderFinanceSummaryBffQueryHandler
    : AizenQueryHandler<GetProviderFinanceSummaryBffQuery, ProviderFinanceSummaryBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    private readonly ILogger<GetProviderFinanceSummaryBffQueryHandler> _logger;

    public GetProviderFinanceSummaryBffQueryHandler(
        IPaymentRemoteCall payment,
        ILogger<GetProviderFinanceSummaryBffQueryHandler> logger)
    {
        _payment = payment;
        _logger  = logger;
    }

    public override async Task<ProviderFinanceSummaryBffResponse?> Handle(
        GetProviderFinanceSummaryBffQuery request, CancellationToken cancellationToken)
    {
        var response = new ProviderFinanceSummaryBffResponse();

        // Fire both calls in parallel — subscription and payout list are independent.
        var subscriptionTask = FetchSubscriptionSafeAsync(request.ProviderProfileId, cancellationToken);
        var payoutsTask      = FetchPayoutsSafeAsync(request.ProviderProfileId, request.PayoutPageSize, cancellationToken);

        await Task.WhenAll(subscriptionTask, payoutsTask);

        var subscription = await subscriptionTask;
        var payouts      = await payoutsTask;

        if (subscription.Warning is not null)
            response.Warnings.Add(subscription.Warning);
        else
            response.ActiveSubscription = subscription.Result;

        if (payouts.Warning is not null)
            response.Warnings.Add(payouts.Warning);
        else if (payouts.Result is not null)
        {
            response.RecentPayouts      = payouts.Result.Items ?? new();
            response.TotalPayoutCount   = payouts.Result.Total;
            response.TotalPayoutAmount  = payouts.Result.Items?.Sum(p => p.NetPayout) ?? 0m;
        }

        return response;
    }

    // ─── private helpers ──────────────────────────────────────────────────────

    private async Task<(Aizen.Modules.Payment.Abstraction.Model.Result.ActiveProviderSubscriptionResult? Result, AdminBffWarning? Warning)>
        FetchSubscriptionSafeAsync(long providerProfileId, CancellationToken ct)
    {
        try
        {
            var sub = await _payment.GetProviderSubscriptionAsync(providerProfileId, ct);
            return (sub, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[ProviderFinanceSummary] Subscription fetch failed for provider {Id}.", providerProfileId);
            return (null, AdminBffWarning.ModuleUnavailable("Payment/Subscription"));
        }
    }

    private async Task<(Aizen.Bff.AdminPanel.Application.Payment.Dto.PaymentPayoutListBffResult? Result, AdminBffWarning? Warning)>
        FetchPayoutsSafeAsync(long providerProfileId, int pageSize, CancellationToken ct)
    {
        try
        {
            var payouts = await _payment.GetPayoutsPagedAsync(
                status:     null,
                providerId: providerProfileId,
                page:       1,
                pageSize:   pageSize,
                ct:         ct);
            return (payouts, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[ProviderFinanceSummary] Payout list fetch failed for provider {Id}.", providerProfileId);
            return (null, AdminBffWarning.ModuleUnavailable("Payment/Payouts"));
        }
    }
}
