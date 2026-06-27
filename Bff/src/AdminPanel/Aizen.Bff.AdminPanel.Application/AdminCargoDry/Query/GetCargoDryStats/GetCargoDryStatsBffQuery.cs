using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryStats;

public sealed class GetCargoDryStatsBffQuery : AizenQuery<GetCargoDryStatsBffResponse>;

public sealed class GetCargoDryStatsBffResponse
{
    public CargoDryStatsBffDto Stats { get; init; } = default!;
}
