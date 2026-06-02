using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Queries;

public sealed class GetLookupItemsByGroupQuery : AizenQuery<IReadOnlyList<LookupItemDto>>
{
    public string GroupCode { get; }
    public bool OnlyActive { get; }

    public GetLookupItemsByGroupQuery(string groupCode, bool onlyActive = true)
    {
        GroupCode = groupCode;
        OnlyActive = onlyActive;
    }
}
