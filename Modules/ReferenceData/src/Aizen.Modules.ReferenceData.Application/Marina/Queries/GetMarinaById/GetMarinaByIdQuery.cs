using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Marina;

namespace Aizen.Modules.ReferenceData.Application.Marina.Queries;

public sealed class GetMarinaByIdQuery : AizenQuery<MarinaDto?>
{
    public long Id { get; }

    public GetMarinaByIdQuery(long id)
    {
        Id = id;
    }
}
