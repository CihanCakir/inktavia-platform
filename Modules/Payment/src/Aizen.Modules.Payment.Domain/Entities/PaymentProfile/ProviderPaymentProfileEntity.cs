using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;

namespace Aizen.Modules.Payment.Domain.Entities.PaymentProfile;

[DocumentationInfo("Provider payment profile entity",
    "Stores a provider's payment/payout identity — Iyzico sub-merchant account details, IBAN (encrypted), and verification status.")]
public sealed class ProviderPaymentProfileEntity : AizenEntityWithAudit
{
    public long    ProviderProfileId    { get; private set; }  // 1:1 with provider
    public string  GatewayProvider      { get; private set; } = default!;  // "manual" | "iyzico"
    public string? SubMerchantKey       { get; private set; }  // Iyzico subMerchantKey (masked in APIs)
    public string? SubMerchantAccountId { get; private set; }  // Iyzico accountId
    public string? IbanEncrypted        { get; private set; }  // AES-256 encrypted IBAN
    public string? LegalName            { get; private set; }
    public string? TaxNumber            { get; private set; }  // Masked in APIs
    public string  Status               { get; private set; } = "Active";  // Active | OnHold | Blocked
    public DateTime? VerifiedAt         { get; private set; }

    private ProviderPaymentProfileEntity() { }

    public static ProviderPaymentProfileEntity Create(
        long providerProfileId, string gatewayProvider,
        string? legalName = null, string? taxNumber = null)
    {
        return new ProviderPaymentProfileEntity
        {
            ProviderProfileId = providerProfileId,
            GatewayProvider   = gatewayProvider,
            LegalName         = legalName,
            TaxNumber         = taxNumber,
            Status            = "Active",
            IsActive          = true,
        };
    }

    public void RegisterSubMerchant(string subMerchantKey, string? accountId)
    {
        SubMerchantKey       = subMerchantKey;
        SubMerchantAccountId = accountId;
        VerifiedAt           = DateTime.UtcNow;
    }

    public void UpdateIban(string ibanEncrypted) => IbanEncrypted = ibanEncrypted;

    public void Suspend()  => Status = "OnHold";
    public void Reactivate() => Status = "Active";
    public void Block()    => Status = "Blocked";
}
