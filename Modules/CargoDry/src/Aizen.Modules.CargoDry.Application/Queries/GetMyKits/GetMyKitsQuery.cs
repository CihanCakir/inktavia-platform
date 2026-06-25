using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetMyKits;

public sealed class GetMyKitsQuery : AizenQuery<List<CargoDryKitDto>>
{
    public long UserId { get; init; }
}
