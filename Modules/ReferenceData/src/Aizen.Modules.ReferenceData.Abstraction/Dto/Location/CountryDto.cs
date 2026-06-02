namespace Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

public sealed class CountryDto
{
    public string CountryCode { get; set; } = default!;
    public string NumericCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string DefaultCurrencyCode { get; set; } = default!;
    public string PhoneCode { get; set; } = default!;
    public bool IsActive { get; set; }
}
