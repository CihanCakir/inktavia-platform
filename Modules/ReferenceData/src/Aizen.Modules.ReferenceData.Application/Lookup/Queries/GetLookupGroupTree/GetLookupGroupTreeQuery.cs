using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Queries;

public sealed class GetLookupGroupTreeQuery : AizenQuery<IReadOnlyList<LookupGroupTreeDto>>
{
    public bool OnlyActive { get; }

    public GetLookupGroupTreeQuery(bool onlyActive = true)
    {
        OnlyActive = onlyActive;
    }
}
