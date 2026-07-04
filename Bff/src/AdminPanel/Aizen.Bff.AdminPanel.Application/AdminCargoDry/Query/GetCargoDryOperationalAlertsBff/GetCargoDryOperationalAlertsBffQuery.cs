using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryOperationalAlertsBff;

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
