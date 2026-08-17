using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryOperationalAlertsBff;

public sealed class GetCargoDryOperationalAlertsBffQuery
    : AizenQuery<GetCargoDryOperationalAlertsBffResponse>
{
    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed class GetCargoDryOperationalAlertsBffResponse
{
    public CargoDryOperationalAlertsBffResponse Alerts { get; init; } = default!;
}
