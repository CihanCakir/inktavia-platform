using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Queries;

public sealed class GetLookupGroupDetailQuery : AizenQuery<LookupGroupDto?>
{
    public long Id { get; }

    public GetLookupGroupDetailQuery(long id)
    {
        Id = id;
    }
}
