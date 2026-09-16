namespace Aizen.Modules.Payment.Abstraction.Request;

/// <summary>Provider self-service KYC submit for async iyzico sub-merchant provisioning. All KYC captured up-front so
/// the consumer can provision without re-prompting; sensitive fields are encrypted at rest.</summary>
public sealed class SubmitProviderPaymentProfileRequest
{
    public string  Iban            { get; set; } = default!;
    public string? LegalName       { get; set; }
    public string? TaxNumber       { get; set; }
    /// <summary>PERSONAL / PRIVATE_COMPANY / LIMITED_OR_JOINT_STOCK_COMPANY.</summary>
    public string? SubMerchantType { get; set; }
    public string? Email           { get; set; }
    public string? TaxOffice       { get; set; }
    public string? GsmNumber       { get; set; }
    public string? ContactName     { get; set; }
    public string? ContactSurname  { get; set; }
    /// <summary>PERSONAL → TC identity number (encrypted at rest).</summary>
    public string? IdentityNumber  { get; set; }
}
