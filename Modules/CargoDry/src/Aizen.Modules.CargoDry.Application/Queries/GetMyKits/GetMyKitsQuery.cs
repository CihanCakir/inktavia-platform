using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.CargoDry.Application.Queries.GetMyKits;

public sealed class GetMyKitsQuery : AizenQuery<GetMyKitsResponse>
{
    public long UserId { get; init; }
}
