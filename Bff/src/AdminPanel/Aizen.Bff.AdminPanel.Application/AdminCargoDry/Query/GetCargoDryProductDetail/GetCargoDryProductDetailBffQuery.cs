using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryProductDetail;

public sealed class GetCargoDryProductDetailBffQuery : AizenQuery<GetCargoDryProductDetailBffResponse?>
{
    public string ProductCode { get; init; } = default!;
}

public sealed class GetCargoDryProductDetailBffResponse
{
    public CargoDryProductBffDto Product { get; init; } = default!;
}
