using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.ProviderEligibility;

namespace Aizen.Modules.Identity.Application.ProviderEligibility.GetProviderAreaAvailability;

/// <summary>
/// M2 — coarse provider availability for a (city, optional canonical category). Returns only the bucketed verdict
/// (available / limited / none); the underlying count and provider ids never leave the handler.
/// </summary>
public sealed class GetProviderAreaAvailabilityQuery : AizenQuery<ProviderAreaAvailabilityDto>
{
    public string  CityCode     { get; init; } = default!;
    public string? CategoryCode { get; init; }
}
