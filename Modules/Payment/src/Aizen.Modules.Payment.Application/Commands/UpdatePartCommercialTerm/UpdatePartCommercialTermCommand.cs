using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Commands.UpdatePartCommercialTerm;

/// <summary>BE-S5a — admin updates an existing part commercial term (scope + version are immutable). Cost is Payment-internal.</summary>
public sealed class UpdatePartCommercialTermCommand : AizenCommand<UpdatePartCommercialTermResult>
{
    public required long          Id { get; init; }
    public decimal                SupplierListPrice    { get; init; }
    public decimal                ProviderDealerMargin { get; init; }
    public decimal                MaxCustomerDiscount       { get; init; }
    public decimal                SupplierFundedAmount      { get; init; }
    public decimal                ProviderFundedAmount      { get; init; }
    public decimal                PlatformFundedAmount      { get; init; }
    public decimal                MinimumProviderReceivable { get; init; }
    public decimal                MaximumDiscountableAmount { get; init; }
    public CommissionRulePriority Priority      { get; init; } = CommissionRulePriority.Standard;
    public required DateTime      EffectiveFrom { get; init; }
    public DateTime?              EffectiveTo   { get; init; }
    public string?                TermName      { get; init; }
    public string?                Notes         { get; init; }
}

public sealed record UpdatePartCommercialTermResult(long Id, string? TermCode);
