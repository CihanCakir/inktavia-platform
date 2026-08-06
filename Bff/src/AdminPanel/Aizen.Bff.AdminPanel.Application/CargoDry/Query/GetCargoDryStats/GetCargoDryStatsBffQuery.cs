using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryStats;

public sealed class GetCargoDryStatsBffQuery : AizenQuery<GetCargoDryStatsBffResponse>;

public sealed class GetCargoDryStatsBffResponse
{
    public CargoDryStatsBffDto Stats { get; init; } = default!;
}
