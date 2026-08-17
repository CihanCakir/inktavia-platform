using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Gateway.Iyzico.Models;

namespace Aizen.Modules.Payment.Application.Gateway.Iyzico;

/// <summary>The three iyzico sub-merchant discriminator values (§5).</summary>
public static class IyzicoSubMerchantType
{
    public const string Personal = "PERSONAL";
    public const string PrivateCompany = "PRIVATE_COMPANY";
    public const string LimitedOrJointStock = "LIMITED_OR_JOINT_STOCK_COMPANY";
}

/// <summary>All KYC data needed to build a type-varied sub-merchant request. No field is defaulted to a fake value.</summary>
public sealed record SubMerchantOnboardingData(
    string  SubMerchantType,
    string  SubMerchantExternalId,
    string  Name,
    string  Email,
    string  Address,
    string? GsmNumber,
    string? ContactName,
    string? ContactSurname,
    string? IdentityNumber,     // TCKN — required for PERSONAL
    string? TaxOffice,
    string? TaxNumber,          // required for LIMITED_OR_JOINT_STOCK_COMPANY
    string? LegalCompanyTitle,
    string? Iban,               // optional at create; required before split-eligibility
    string  ConversationId,
    string  Locale = "tr",
    string  Currency = "TRY");

/// <summary>
/// BE-P9-fix §5 — builds a <see cref="IyzicoSubMerchantRequest"/> for the chosen <c>subMerchantType</c>, serialising ONLY
/// the fields that type requires and <b>failing loud</b> (no hardcoded TCKN) when a required field is missing.
/// </summary>
public static class IyzicoSubMerchantRequestBuilder
{
    public static IyzicoSubMerchantRequest Build(SubMerchantOnboardingData d)
    {
        Require(d.SubMerchantExternalId, "subMerchantExternalId");
        Require(d.Name, "name");
        Require(d.Email, "email");
        Require(d.Address, "address");

        var req = new IyzicoSubMerchantRequest
        {
            Locale                = d.Locale,
            ConversationId        = d.ConversationId,
            SubMerchantType       = d.SubMerchantType,
            SubMerchantExternalId = d.SubMerchantExternalId,
            Name                  = d.Name,
            Email                 = d.Email,
            Address               = d.Address,
            GsmNumber             = d.GsmNumber,
            Currency              = d.Currency,
            Iban                  = string.IsNullOrWhiteSpace(d.Iban) ? null : d.Iban,
        };

        switch (d.SubMerchantType)
        {
            case IyzicoSubMerchantType.Personal:
                req.ContactName    = Require(d.ContactName, "contactName");
                req.ContactSurname = Require(d.ContactSurname, "contactSurname");
                req.IdentityNumber = Require(d.IdentityNumber, "identityNumber (TCKN)");
                Require(d.GsmNumber, "gsmNumber");
                break;

            case IyzicoSubMerchantType.PrivateCompany:
                req.TaxOffice         = Require(d.TaxOffice, "taxOffice");
                req.LegalCompanyTitle = Require(d.LegalCompanyTitle, "legalCompanyTitle");
                Require(d.GsmNumber, "gsmNumber");
                req.TaxNumber         = d.TaxNumber;   // optional for PRIVATE_COMPANY
                break;

            case IyzicoSubMerchantType.LimitedOrJointStock:
                req.TaxOffice         = Require(d.TaxOffice, "taxOffice");
                req.TaxNumber         = Require(d.TaxNumber, "taxNumber");
                req.LegalCompanyTitle = Require(d.LegalCompanyTitle, "legalCompanyTitle");
                Require(d.GsmNumber, "gsmNumber");
                break;

            default:
                throw new AizenBusinessException((int)PaymentErrorCode.SubMerchantRegistrationFailed,
                    $"Unknown subMerchantType '{d.SubMerchantType}'.");
        }

        return req;
    }

    private static string Require(string? value, string field)
        => string.IsNullOrWhiteSpace(value)
            ? throw new AizenBusinessException((int)PaymentErrorCode.SubMerchantRegistrationFailed,
                $"Sub-merchant onboarding: required field '{field}' is missing.")
            : value!;
}
