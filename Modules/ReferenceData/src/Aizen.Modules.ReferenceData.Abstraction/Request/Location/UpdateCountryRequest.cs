namespace Aizen.Modules.ReferenceData.Abstraction.Request.Location;

public sealed class UpdateCountryRequest
{
    public string CountryCode { get; set; } = default!;
    public string DefaultCurrencyCode { get; set; } = default!;
    public string PhoneCode { get; set; } = default!;
    public bool IsActive { get; set; }
}
