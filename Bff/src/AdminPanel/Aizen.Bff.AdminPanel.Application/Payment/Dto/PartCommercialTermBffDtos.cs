namespace Aizen.Bff.AdminPanel.Application.Payment.Dto;

// ─── BE-S5 PartCommercialTerm — BFF DTOs (ADMIN-ONLY, cost-bearing) ───────────
// This is the ONLY BFF surface that carries the confidential cost fields (SupplierListPrice, ProviderDealerMargin).
// The admin manages these; they must NOT leak to any provider/customer-facing payload (§20.9 confidentiality).
// Enum fields (Priority, Status) are `string` per the BFF Newtonsoft contract (the Payment module serializes enums as names).

/// <summary>
/// Create (append a new version of) a part commercial term. Versioning is a side effect of create — the module computes the
/// next version for the scope and appends a new row; it never mutates an active one.
/// </summary>
public sealed record CreatePartCommercialTermBffRequest(
    string?   Brand,                       // scope
    string?   ProductCode,                 // scope
    long?     ProviderProfileId,           // scope
    string?   CategoryCode,                // scope
    string    CurrencyCode,                // scope
    decimal   SupplierListPrice,           // CONFIDENTIAL cost — admin-only
    decimal   ProviderDealerMargin,        // CONFIDENTIAL margin — admin-only
    decimal   MaxCustomerDiscount,
    decimal   SupplierFundedAmount,
    decimal   ProviderFundedAmount,
    decimal   PlatformFundedAmount,
    decimal   MinimumProviderReceivable,
    decimal   MaximumDiscountableAmount,
    string    Priority,                    // CommissionRulePriority name (Low|Standard|Medium|High|EMERGENCY)
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string?   TermName,
    string?   Notes);

/// <summary>Update a part commercial term in place. <c>Id</c> forced from the route; scope + version are immutable on update.</summary>
public sealed record UpdatePartCommercialTermBffRequest(
    long      Id,
    decimal   SupplierListPrice,
    decimal   ProviderDealerMargin,
    decimal   MaxCustomerDiscount,
    decimal   SupplierFundedAmount,
    decimal   ProviderFundedAmount,
    decimal   PlatformFundedAmount,
    decimal   MinimumProviderReceivable,
    decimal   MaximumDiscountableAmount,
    string    Priority,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string?   TermName,
    string?   Notes);

public sealed record PartCommercialTermCreateBffResult(long Id, string? TermCode, int Version);
public sealed record PartCommercialTermUpdateBffResult(long Id, string? TermCode);

/// <summary>Deactivate / reactivate outcome (the module returns a bare bool; wrapped so the outward envelope can carry it).</summary>
public sealed record PartCommercialTermDeactivateResult(bool Success);

// ─── List / Detail (admin surface — carries confidential cost) ───────────────

/// <summary>A part commercial term as it appears in the admin list / detail. Cost fields are admin-only.</summary>
public sealed record PartCommercialTermBffDto(
    long      Id,
    string?   TermCode,
    int       Version,
    string?   Brand,                       // scope
    string?   ProductCode,                 // scope
    long?     ProviderProfileId,           // scope
    string?   CategoryCode,                // scope
    string    CurrencyCode,                // scope
    decimal   SupplierListPrice,           // CONFIDENTIAL cost — admin-only
    decimal   ProviderDealerMargin,        // CONFIDENTIAL margin — admin-only
    decimal   MaxCustomerDiscount,
    decimal   SupplierFundedAmount,
    decimal   ProviderFundedAmount,
    decimal   PlatformFundedAmount,
    decimal   MinimumProviderReceivable,
    decimal   MaximumDiscountableAmount,
    string    Priority,                    // CommissionRulePriority name
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string    Status,                      // "Active" | "Scheduled" | "Expired" | "Inactive" | "Draft"
    bool      IsActive,
    string?   TermName,
    string?   Notes,
    long?     CreateUserId,
    DateTime? CreateDate,
    long?     ModifyUserId,
    DateTime? ModifyDate
);

/// <summary>Part commercial term list response (no paging — terms are few).</summary>
public sealed record PartCommercialTermListBffResult(
    List<PartCommercialTermBffDto> Items,
    int                            Total
);
