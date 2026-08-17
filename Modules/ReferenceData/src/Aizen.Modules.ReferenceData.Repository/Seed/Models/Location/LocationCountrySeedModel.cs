
namespace Aizen.Modules.ReferenceData.Repository.Seed.Models.Location;

/// <summary>Seed model for a LocationCountry document, read from country.json.</summary>
[DocumentationInfo("Seed model representing a country location document loaded from JSON.", "Maps to LocationCountryDocument. Idempotency key: CountryCode. Name supports multilingual keys.")]
public sealed class LocationCountrySeedModel
{
    public string CountryCode { get; set; } = default!;
    public string NumericCode { get; set; } = default!;
    public Dictionary<string, string> Name { get; set; } = new();
    public string DefaultCurrencyCode { get; set; } = default!;
    public string PhoneCode { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
