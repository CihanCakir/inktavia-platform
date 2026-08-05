using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.PartCommercialTerm;

namespace Aizen.Modules.Payment.Application.Dto;

/// <summary>
/// BE-S5a — Application-layer DTO for the <b>admin</b> part-commercial-term surface (list + detail). Admin-only
/// (<c>[Authorize(Roles="Admin")]</c>), Payment-internal — the admin configures the confidential cost, so this admin DTO does
/// carry <see cref="SupplierListPrice"/> / <see cref="ProviderDealerMargin"/>. This is <b>not</b> the boundary-crossing projection:
/// the only thing that leaves Payment to SR/FE is the cost-free <c>PartLineAllowanceDto</c>. Cost is never logged.
/// </summary>
public sealed record PartCommercialTermDto(
    long                   Id,
    string?                TermCode,
    int                    Version,
    // ── Scope ──
    string?                Brand,
    string?                ProductCode,
    long?                  ProviderProfileId,
    string?                CategoryCode,
    string                 CurrencyCode,
    // ── Confidential cost / margin (admin-only) ──
    decimal                SupplierListPrice,
    decimal                ProviderDealerMargin,
    // ── Caps + funded split ──
    decimal                MaxCustomerDiscount,
    decimal                SupplierFundedAmount,
    decimal                ProviderFundedAmount,
    decimal                PlatformFundedAmount,
    decimal                MinimumProviderReceivable,
    decimal                MaximumDiscountableAmount,
    // ── Lifecycle / admin ──
    CommissionRulePriority Priority,
    DateTime               EffectiveFrom,
    DateTime?              EffectiveTo,
    CommissionRuleStatus   Status,
    bool                   IsActive,
    string?                TermName,
    string?                Notes,
    long?                  CreateUserId,
    DateTime?              CreateDate,
    long?                  ModifyUserId,
    DateTime?              ModifyDate);

/// <summary>Part commercial term list (no paging — terms are few; mirrors the P3/P5/P6 admin surface).</summary>
public sealed record PartCommercialTermListResult(
    List<PartCommercialTermDto> Items,
    int                         Total);

public static class PartCommercialTermDtoMapper
{
    public static PartCommercialTermDto ToDto(PartCommercialTermEntity e) => new(
        e.Id, e.TermCode, e.Version,
        e.Brand, e.ProductCode, e.ProviderProfileId, e.CategoryCode, e.CurrencyCode,
        e.SupplierListPrice, e.ProviderDealerMargin,
        e.MaxCustomerDiscount, e.SupplierFundedAmount, e.ProviderFundedAmount, e.PlatformFundedAmount,
        e.MinimumProviderReceivable, e.MaximumDiscountableAmount,
        e.Priority, e.EffectiveFrom, e.EffectiveTo, e.Status, e.IsActive, e.TermName, e.Notes,
        e.CreateUserId, e.CreateDate, e.ModifyUserId, e.ModifyDate);
}
