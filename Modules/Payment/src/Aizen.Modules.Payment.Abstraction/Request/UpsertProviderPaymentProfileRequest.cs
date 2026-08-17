namespace Aizen.Modules.Payment.Abstraction.Request;

public sealed class UpsertProviderPaymentProfileRequest
{
    public string  Iban       { get; set; } = default!;
    public string? LegalName  { get; set; }
    public string? TaxNumber  { get; set; }

    // ── BE-I1 type-varied KYC (additive; all optional — the legacy body above stays valid) ──
    /// <summary>PERSONAL / PRIVATE_COMPANY / LIMITED_OR_JOINT_STOCK_COMPANY. Drives which fields below are required (P9-fix §5).</summary>
    public string? SubMerchantType   { get; set; }
    /// <summary>PERSONAL → TC identity number.</summary>
    public string? IdentityNumber    { get; set; }
    /// <summary>PRIVATE_COMPANY / LIMITED_OR_JOINT_STOCK_COMPANY → tax office.</summary>
    public string? TaxOffice         { get; set; }
    /// <summary>PRIVATE_COMPANY / LIMITED_OR_JOINT_STOCK_COMPANY → registered legal company title.</summary>
    public string? LegalCompanyTitle { get; set; }
}
