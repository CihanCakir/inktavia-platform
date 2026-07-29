using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.SubscribeProviderPlan;

/// <summary>
/// Subscribes a provider to a plan for one billing period.
/// Creates a ProviderPlanSubscriptionEntity with Active status.
///
/// PaymentTransactionId is optional — free plans (MonthlyPrice = 0) have no transaction.
/// For paid plans, the escrow must be created and captured beforehand.
/// </summary>
public sealed class SubscribeProviderPlanCommand : AizenCommand<SubscribeProviderPlanResult>
{
    public required long    ProviderProfileId     { get; init; }
    public required long    ProviderPlanId        { get; init; }
    public required decimal PaidAmount            { get; init; }
    public required string  CurrencyCode          { get; init; }
    public required DateTime PeriodStart          { get; init; }
    public required DateTime PeriodEnd            { get; init; }
    public bool             AutoRenew             { get; init; } = false;
    public long?            PaymentTransactionId  { get; init; }

    /// <summary>Billing cadence used to resolve the authoritative ProviderPlanPrice (BE-P4). Defaults to Monthly.</summary>
    public BillingPeriod    BillingPeriod         { get; init; } = BillingPeriod.Monthly;
}
