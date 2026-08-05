using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Commands.CreatePartCommercialTerm;

/// <summary>BE-S5a — admin creates a new (versioned) part commercial term for a scope. Cost is admin-configured, Payment-internal.</summary>
public sealed class CreatePartCommercialTermCommand : AizenCommand<CreatePartCommercialTermResult>
{
    // ── Scope ──
    public string?                Brand             { get; init; }
    public string?                ProductCode       { get; init; }
    public long?                  ProviderProfileId { get; init; }
    public string?                CategoryCode      { get; init; }
    public string                 CurrencyCode      { get; init; } = "TRY";
    // ── Confidential cost / margin ──
    public decimal                SupplierListPrice    { get; init; }
    public decimal                ProviderDealerMargin { get; init; }
    // ── Caps + funded split ──
    public decimal                MaxCustomerDiscount       { get; init; }
    public decimal                SupplierFundedAmount      { get; init; }
    public decimal                ProviderFundedAmount      { get; init; }
    public decimal                PlatformFundedAmount      { get; init; }
    public decimal                MinimumProviderReceivable { get; init; }
    public decimal                MaximumDiscountableAmount { get; init; }
    // ── Lifecycle ──
    public CommissionRulePriority Priority      { get; init; } = CommissionRulePriority.Standard;
    public required DateTime      EffectiveFrom { get; init; }
    public DateTime?              EffectiveTo   { get; init; }
    public string?                TermName      { get; init; }
    public string?                Notes         { get; init; }
}

public sealed record CreatePartCommercialTermResult(long Id, string TermCode, int Version);
