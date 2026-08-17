using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryProducts;

public sealed class GetCargoDryProductsBffQuery : AizenQuery<GetCargoDryProductsBffResponse>;

public sealed class GetCargoDryProductsBffResponse
{
    public List<CargoDryProductBffDto> Products { get; init; } = [];
}
