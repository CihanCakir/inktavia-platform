using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;

namespace Aizen.Modules.Payment.Domain.Entities.Plan;

[DocumentationInfo("Participant plan entity",
    "Admin-managed participant/customer subscription plan. Provides service discounts and InkCoin earn multipliers.")]
public sealed class ParticipantPlanEntity : AizenEntityWithAudit
{
    public string  PlanCode                 { get; private set; } = default!;  // BASIC / GOLD / PLATINUM
    public string  Name                     { get; private set; } = default!;
    public string? Description              { get; private set; }
    public decimal MonthlyPriceTRY          { get; private set; }
    public decimal ServiceDiscountRate      { get; private set; }  // e.g. 0.05 = 5%
    public decimal CargoDryDiscountRate     { get; private set; }  // e.g. 0.10 = 10%
    public decimal InkCoinEarnMultiplier    { get; private set; }  // 1.0 = normal; 2.0 = double
    public int     SortOrder                { get; private set; }
    public DateTime? ValidFrom              { get; private set; }
    public DateTime? ValidTo                { get; private set; }

    private ParticipantPlanEntity() { }

    public static ParticipantPlanEntity Create(
        string planCode, string name, string? description,
        decimal monthlyPriceTRY, decimal serviceDiscountRate,
        decimal cargoDryDiscountRate, decimal inkCoinEarnMultiplier, int sortOrder)
    {
        return new ParticipantPlanEntity
        {
            PlanCode              = planCode.ToUpperInvariant(),
            Name                  = name,
            Description           = description,
            MonthlyPriceTRY       = monthlyPriceTRY,
            ServiceDiscountRate   = serviceDiscountRate,
            CargoDryDiscountRate  = cargoDryDiscountRate,
            InkCoinEarnMultiplier = inkCoinEarnMultiplier,
            SortOrder             = sortOrder,
            IsActive              = true,
        };
    }

    public void Update(string name, string? description, decimal monthlyPriceTRY,
        decimal serviceDiscountRate, decimal cargoDryDiscountRate,
        decimal inkCoinEarnMultiplier, int sortOrder)
    {
        Name                  = name;
        Description           = description;
        MonthlyPriceTRY       = monthlyPriceTRY;
        ServiceDiscountRate   = serviceDiscountRate;
        CargoDryDiscountRate  = cargoDryDiscountRate;
        InkCoinEarnMultiplier = inkCoinEarnMultiplier;
        SortOrder             = sortOrder;
    }

    public bool IsFree => MonthlyPriceTRY == 0;
}
