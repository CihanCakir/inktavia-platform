using System.Text.Json;
using Aizen.Core.Security;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// The iyzico KYC fields that are NOT persisted as columns on <c>ProviderPaymentProfileEntity</c> — captured at
/// data-submit, serialized to JSON and AES-encrypted into <c>KycPayloadEncrypted</c> so the ASYNC provisioning
/// consumer can rebuild the canonical register call without carrying PII on the bus. IBAN / LegalName / TaxNumber
/// live as their own (existing) columns and are read from the entity, not from here.
/// </summary>
public sealed record SubMerchantKycPayload(
    string? Email,
    string? SubMerchantType,
    string? TaxOffice,
    string? GsmNumber,
    string? ContactName,
    string? ContactSurname,
    string? IdentityNumber)
{
    public string Encrypt(string encryptionKey)
        => AizenSecurityHelper.EncryptByAES(JsonSerializer.Serialize(this), encryptionKey);

    /// <summary>Decrypts a stored blob; returns an all-null payload when absent/corrupt (the consumer falls back to entity columns).</summary>
    public static SubMerchantKycPayload Decrypt(string? encrypted, string encryptionKey)
    {
        if (string.IsNullOrEmpty(encrypted)) return Empty;
        try
        {
            return JsonSerializer.Deserialize<SubMerchantKycPayload>(
                AizenSecurityHelper.DecryptByAES(encrypted, encryptionKey)) ?? Empty;
        }
        catch
        {
            return Empty;
        }
    }

    public static readonly SubMerchantKycPayload Empty = new(null, null, null, null, null, null, null);
}
