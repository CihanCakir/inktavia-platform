using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryWarehouseOptions;

public sealed class GetCargoDryWarehouseOptionsBffQuery
    : AizenQuery<GetCargoDryWarehouseOptionsBffResponse>
{
}

public sealed class GetCargoDryWarehouseOptionsBffResponse
{
    public List<CargoDryWarehouseOptionBffDto> Warehouses { get; init; } = [];
}
