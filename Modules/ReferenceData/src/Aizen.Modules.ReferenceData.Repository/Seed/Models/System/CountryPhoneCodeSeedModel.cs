using Aizen.Modules.ReferenceData.Abstraction.Model;

namespace Aizen.Modules.ReferenceData.Repository.Seed.Models.System;

/// <summary>Seed model for a CountryPhoneCode entity, read from country-phone-codes.json.</summary>
[DocumentationInfo("Seed model representing a country phone code loaded from JSON.", "Maps to CountryPhoneCodeEntity. Idempotency key: CountryCode.")]
public sealed class CountryPhoneCodeSeedModel
{
    public string CountryCode { get; set; } = default!;
    public string PhoneCode { get; set; } = default!;
    public string CountryName { get; set; } = default!;
    public bool IsAllowedForRegistration { get; set; }
    public bool IsAllowedForTransfer { get; set; }
    public bool IsActive { get; set; } = true;
}
