using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryProducts;

public sealed class GetCargoDryProductsBffQuery : AizenQuery<GetCargoDryProductsBffResponse>;

public sealed class GetCargoDryProductsBffResponse
{
    public List<CargoDryProductBffDto> Products { get; init; } = [];
}
