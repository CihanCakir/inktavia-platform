using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Domain.Entities.PaymentProfile;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>Single source of truth for the provider payment-profile → DTO projection (masking + onboarding + attempt error).</summary>
public static class ProviderPaymentProfileMapper
{
    public static ProviderPaymentProfileDto ToDto(ProviderPaymentProfileEntity e, string? ibanLast4Override = null)
    {
        var last4 = ibanLast4Override ?? e.IbanLast4;
        return new ProviderPaymentProfileDto
        {
            GatewayProvider      = e.GatewayProvider,
            HasIban              = !string.IsNullOrEmpty(e.IbanEncrypted),
            IbanMasked           = string.IsNullOrEmpty(last4) ? null : $"TR** **** **** {last4}",
            LegalName            = e.LegalName,
            TaxNumberMasked      = MaskTaxNumber(e.TaxNumber),
            Status               = e.Status,
            VerifiedAt           = e.VerifiedAt,
            IsSplitEligible      = e.IsSplitEligible,
            OnboardingStatus     = e.OnboardingStatus.ToString(),
            SubMerchantKeyMasked = MaskSubMerchantKey(e.SubMerchantKey),
            IbanRequired         = string.IsNullOrEmpty(e.IbanEncrypted),
            RejectionReason      = null,
            LastAttemptError     = e.LastAttemptError,
        };
    }

    private static string? MaskSubMerchantKey(string? key)
        => string.IsNullOrEmpty(key) ? null : (key.Length <= 4 ? new string('*', key.Length) : $"****{key[^4..]}");

    private static string? MaskTaxNumber(string? taxNumber)
    {
        if (string.IsNullOrEmpty(taxNumber)) return null;
        if (taxNumber.Length <= 3) return new string('*', taxNumber.Length);
        return new string('*', taxNumber.Length - 3) + taxNumber[^3..];
    }
}
