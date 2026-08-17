using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Application.Services;

namespace Aizen.Modules.Payment.Application.Queries.ResolveCustomerDiscount;

[DocumentationInfo("ResolveCustomerDiscountQueryHandler",
    "Resolves the customer discount + funding split + benefit budget remaining (the P5-engine customer-side inputs). " +
    "May throw CustomerDiscountRuleConflict on a fail-loud specificity tie.")]
public sealed class ResolveCustomerDiscountQueryHandler
    : AizenQueryHandler<ResolveCustomerDiscountQuery, CustomerDiscountBenefitInputs>
{
    private readonly CustomerDiscountBenefitService _service;

    public ResolveCustomerDiscountQueryHandler(CustomerDiscountBenefitService service) => _service = service;

    public override async Task<CustomerDiscountBenefitInputs?> Handle(
        ResolveCustomerDiscountQuery request, CancellationToken ct)
        => await _service.AssembleAsync(
            request.CustomerPlanId, request.CategoryCode, request.CurrencyCode,
            request.ServiceBaseAmount, request.ProviderConsent, request.ParticipantPlanSubscriptionId, ct);
}
