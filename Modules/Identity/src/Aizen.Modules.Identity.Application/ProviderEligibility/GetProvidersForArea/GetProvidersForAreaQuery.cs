using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.ProviderEligibility;

namespace Aizen.Modules.Identity.Application.ProviderEligibility.GetProvidersForArea;

/// <summary>
/// I2 read-model — active + approved provider profiles operating in <see cref="CityCode"/> (and, if given,
/// serving <see cref="CategoryCode"/>). MVP city-level. Capped by <see cref="Take"/>.
/// </summary>
public sealed class GetProvidersForAreaQuery : AizenListedQuery<ProviderForAreaDto>
{
    public string  CityCode     { get; init; } = default!;
    public string? CategoryCode { get; init; }
    public int     Take         { get; init; } = 500;
}
