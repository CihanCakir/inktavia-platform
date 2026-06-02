using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class UpdateCountryCommand : AizenCommand<CountryDto>
{
    public string CountryCode { get; }
    public string DefaultCurrencyCode { get; }
    public string PhoneCode { get; }
    public bool IsActive { get; }

    public UpdateCountryCommand(string countryCode, string defaultCurrencyCode, string phoneCode, bool isActive)
    {
        CountryCode = countryCode;
        DefaultCurrencyCode = defaultCurrencyCode;
        PhoneCode = phoneCode;
        IsActive = isActive;
    }
}
