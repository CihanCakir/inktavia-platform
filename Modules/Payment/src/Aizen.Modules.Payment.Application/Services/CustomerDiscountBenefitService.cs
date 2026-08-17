using Aizen.Modules.Payment.Domain.Entities.CustomerDiscount;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// Assembles the customer-side inputs the P5 <c>ProfitProtectionEngine</c> needs (§5): resolves the authoritative
/// <see cref="CustomerDiscountRuleEntity"/>, computes the requested discount + funding split, and reads the benefit
/// budget remaining. Pure orchestration over the resolvers — no writes. Actual wiring into acceptance and the
/// reserve/consume ordering are P8; <c>CustomerPlanRevenueAllocation</c> is populated by P8 (0 here).
/// </summary>
public sealed class CustomerDiscountBenefitService
{
    private readonly ICustomerDiscountRuleRepository _discounts;
    private readonly ICustomerBenefitBudgetRepository _budgets;

    public CustomerDiscountBenefitService(
        ICustomerDiscountRuleRepository discounts, ICustomerBenefitBudgetRepository budgets)
    {
        _discounts = discounts;
        _budgets   = budgets;
    }

    public async Task<CustomerDiscountBenefitInputs> AssembleAsync(
        long?    customerPlanId,
        string?  categoryCode,
        string   currencyCode,
        decimal  serviceBaseAmount,
        bool     providerConsent,
        long?    participantPlanSubscriptionId,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var rule = await _discounts.ResolveAsync(
            new CustomerDiscountResolveContext(customerPlanId, categoryCode, currencyCode), now, ct);

        var requested   = rule?.ComputeRequestedDiscount(serviceBaseAmount) ?? 0m;
        var allocation  = rule is null
            ? new CustomerDiscountFundingAllocation(0m, 0m, 0m, 0m, 0m)
            : CustomerDiscountFundingCalculator.Allocate(requested, rule, providerConsent);

        decimal budgetRemaining = 0m;
        if (participantPlanSubscriptionId is { } subId)
        {
            var budget = await _budgets.GetActiveBySubscriptionAsync(subId, now, ct);
            budgetRemaining = budget?.RemainingAmount ?? 0m;
        }

        return new CustomerDiscountBenefitInputs(
            DiscountRuleId:                          rule?.Id,
            RequestedDiscountAmount:                 requested,
            RequestedPlatformFundedCustomerDiscount: allocation.PlatformFundedAmount,
            ProviderFundedCustomerDiscount:          allocation.ProviderFundedAmount,
            AppliedDiscountAmount:                   allocation.AppliedDiscountAmount,
            UnappliedDueToConsent:                   allocation.UnappliedDueToConsent,
            CustomerBenefitBudgetRemaining:          budgetRemaining,
            CustomerPlanRevenueAllocation:           0m);   // P8 populates
    }
}

/// <summary>The customer-side inputs handed to the P5 profit-protection engine.</summary>
public sealed record CustomerDiscountBenefitInputs(
    long?   DiscountRuleId,
    decimal RequestedDiscountAmount,
    decimal RequestedPlatformFundedCustomerDiscount,
    decimal ProviderFundedCustomerDiscount,
    decimal AppliedDiscountAmount,
    decimal UnappliedDueToConsent,
    decimal CustomerBenefitBudgetRemaining,
    decimal CustomerPlanRevenueAllocation);
