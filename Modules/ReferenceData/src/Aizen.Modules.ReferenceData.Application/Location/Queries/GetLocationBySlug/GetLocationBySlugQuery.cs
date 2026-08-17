using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

public sealed class GetLocationBySlugQuery : AizenQuery<LocationBySlugDto?>
{
    public string Slug { get; }

    public GetLocationBySlugQuery(string slug) => Slug = slug;
}
