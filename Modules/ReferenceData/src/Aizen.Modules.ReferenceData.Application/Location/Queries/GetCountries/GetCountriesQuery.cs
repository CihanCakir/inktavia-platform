using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

public sealed class GetCountriesQuery : AizenQuery<IReadOnlyList<CountryDto>>
{
    public bool OnlyActive { get; }

    public GetCountriesQuery(bool onlyActive = true)
    {
        OnlyActive = onlyActive;
    }
}
