using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Queries;

public sealed class GetLookupGroupListQuery : AizenQuery<IReadOnlyList<LookupGroupDto>>
{
    public bool OnlyActive { get; }

    public GetLookupGroupListQuery(bool onlyActive = true)
    {
        OnlyActive = onlyActive;
    }
}
