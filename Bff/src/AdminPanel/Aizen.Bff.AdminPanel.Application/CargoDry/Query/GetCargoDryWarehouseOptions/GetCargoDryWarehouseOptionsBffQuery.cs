using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryWarehouseOptions;

public sealed class GetCargoDryWarehouseOptionsBffQuery
    : AizenQuery<GetCargoDryWarehouseOptionsBffResponse>
{
}

public sealed class GetCargoDryWarehouseOptionsBffResponse
{
    public List<CargoDryWarehouseOptionBffDto> Warehouses { get; init; } = [];
}
