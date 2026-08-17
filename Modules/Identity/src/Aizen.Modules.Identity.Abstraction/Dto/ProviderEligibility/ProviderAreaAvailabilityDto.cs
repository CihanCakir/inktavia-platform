namespace Aizen.Modules.Identity.Abstraction.Dto.ProviderEligibility;

/// <summary>
/// M2 — the COARSE provider availability signal for a (city, category) pair. Deliberately carries ONLY the bucketed
/// verdict: no provider count, no provider ids/user ids, no names, no scores, no ranking. The count is bucketed
/// inside the handler and never leaves the Identity boundary. <see cref="Availability"/> is one of
/// <c>"available"</c> / <c>"limited"</c> / <c>"none"</c>.
/// </summary>
public sealed class ProviderAreaAvailabilityDto
{
    public string  CityCode     { get; set; } = default!;
    public string? CategoryCode { get; set; }
    public string  Availability { get; set; } = default!;
}
