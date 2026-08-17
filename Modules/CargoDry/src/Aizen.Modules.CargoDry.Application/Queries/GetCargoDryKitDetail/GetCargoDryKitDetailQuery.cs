using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryKitDetail;

public sealed class GetCargoDryKitDetailQuery : AizenQuery<GetCargoDryKitDetailResponse>
{
    public long KitId { get; init; }
}

public sealed class GetCargoDryKitDetailResponse
{
    public CargoDryKitDetailDto? Kit { get; init; }
}
