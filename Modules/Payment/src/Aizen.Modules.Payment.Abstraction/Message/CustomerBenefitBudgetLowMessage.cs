using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// N4 (§19.7) — published fire-and-forget by <c>CustomerBenefitBudgetService</c> when a customer-benefit budget's
/// remaining crosses the configurable low threshold (or hits 0) — once per crossing (a marker re-arms on recovery).
/// The Notification module alerts admins so ops can top up before customer discounts silently stop.
/// </summary>
[DocumentationInfo("Customer benefit budget low message", "A benefit budget crossed the low/exhausted threshold (N4).")]
public sealed class CustomerBenefitBudgetLowMessage : AizenBaseMessage
{
    public long    BudgetId        { get; init; }
    public long    CustomerPlanId  { get; init; }
    public decimal RemainingAmount { get; init; }
    public decimal FundedAmount    { get; init; }
    public decimal ThresholdAmount { get; init; }
    public decimal ThresholdPercent { get; init; }
    public string  CurrencyCode    { get; init; } = "TRY";
    public bool    IsExhausted     { get; init; }
}
