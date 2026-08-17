using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Application.Services;

namespace Aizen.Modules.Payment.Application.Queries.ResolveCustomerDiscount;

/// <summary>
/// Admin/dev: resolves the applicable customer discount for a context and returns the requested amount + funding split
/// + benefit-budget remaining — i.e. the customer-side inputs the P5 profit-protection engine consumes (§5).
/// </summary>
public sealed class ResolveCustomerDiscountQuery : AizenQuery<CustomerDiscountBenefitInputs>
{
    public long?    CustomerPlanId                { get; init; }
    public string?  CategoryCode                  { get; init; }
    public string   CurrencyCode                  { get; init; } = "TRY";
    public required decimal ServiceBaseAmount     { get; init; }
    public bool     ProviderConsent               { get; init; }
    public long?    ParticipantPlanSubscriptionId { get; init; }
}
