using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryOperationalOverviewBff;

public sealed class GetCargoDryOperationalOverviewBffQuery
    : AizenQuery<GetCargoDryOperationalOverviewBffResponse>
{
    // No parameters — full overview snapshot.
}

public sealed class GetCargoDryOperationalOverviewBffResponse
{
    public CargoDryOperationalOverviewBffDto Overview { get; init; } = default!;
}
