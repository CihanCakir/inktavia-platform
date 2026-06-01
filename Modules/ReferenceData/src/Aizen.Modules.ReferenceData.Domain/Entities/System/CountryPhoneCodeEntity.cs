using Aizen.Core.Domain;

namespace Aizen.Modules.ReferenceData.Domain.Entities.System;

public sealed class CountryPhoneCodeEntity : AizenEntityWithAudit
{
    public string CountryCode { get; private set; } = default!;
    public string PhoneCode { get; private set; } = default!;
    public string CountryName { get; private set; } = default!;
    public bool IsAllowedForRegistration { get; private set; }
    public bool IsAllowedForTransfer { get; private set; }

    public CountryPhoneCodeEntity() { }

    public static CountryPhoneCodeEntity Create(string countryCode, string phoneCode, string countryName, bool isAllowedForRegistration, bool isAllowedForTransfer)
    {
        return new CountryPhoneCodeEntity
        {
            CountryCode = countryCode.Trim().ToUpperInvariant(),
            PhoneCode = phoneCode.Trim(),
            CountryName = countryName.Trim(),
            IsAllowedForRegistration = isAllowedForRegistration,
            IsAllowedForTransfer = isAllowedForTransfer,
            IsActive = true
        };
    }
}
