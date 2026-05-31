namespace Aizen.Modules.ReferenceData.Abstraction.Request.Location;

public sealed class CreateCountryRequest
{
    public string CountryCode { get; set; } = default!;
    public string NumericCode { get; set; } = default!;
    public Dictionary<string, string> Name { get; set; } = new();
    public string DefaultCurrencyCode { get; set; } = default!;
    public string PhoneCode { get; set; } = default!;
}
